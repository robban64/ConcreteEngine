#region

using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderType
{
    Render2D,
    Render3D
}

public interface IRenderSystem : IGameEngineSystem
{
    ICamera Camera { get; }

    TSink GetSink<TSink>() where TSink : IDrawSink;
    Material CreateMaterial(string templateName);
    void MutateRenderPass(RenderTargetId targetId);

    public SceneRenderProperties SceneRenderProps { get; }
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly IGraphicsRuntime _graphics;
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;

    private RenderRegistry _renderRegistry;
    private RenderPassRegistry _renderPassRegistry;

    private DrawCommandCollector _commandCollector = null!;
    private RenderPipeline _pipeline = null!;
    private DrawProcessor _drawProcessor = null!;
    private DrawUniforms _drawUniforms = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private SceneDrawProducer _sceneDrawProducer = null!;
    private CommandProducerContext _cmdProducerCtx = null!;

    private bool _initialized = false;

    private FrameInfo _frameCtx;

    private RenderGlobalSnapshot _snapshot;

    public SceneRenderProperties SceneRenderProps { get; }

    public ICamera Camera { get; } = new Camera3D();

    internal RenderRegistry RenderRegistry => _renderRegistry;

    internal RenderSystem(IGraphicsRuntime graphics, Size2D outputSize)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _gfxCmd = graphics.Gfx.Commands;
        SceneRenderProps = new SceneRenderProperties();
        SceneRenderProps.SetOutputSize(outputSize);
        SceneRenderProps.Commit();
        _snapshot = SceneRenderProps.CurrentSnapshot;
    }

    internal void InitializeGraphics(IReadOnlyList<Shader> shaders)
    {
        InvalidOpThrower.ThrowIf(_snapshot.OutputSize.Width <= 1);
        InvalidOpThrower.ThrowIf(_snapshot.OutputSize.Height <= 1);

        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_snapshot.OutputSize);
        _renderRegistry.RegisterUniformBuffer<FrameUniformRecord>();
        _renderRegistry.RegisterUniformBuffer<CameraUniformRecord>();
        _renderRegistry.RegisterUniformBuffer<DirLightUniformRecord>();
        _renderRegistry.RegisterUniformBuffer<MaterialUniformRecord>();
        _renderRegistry.RegisterUniformBuffer<DrawObjectUniform>();
        _renderRegistry.RegisterUniformBuffer<FramePostProcessUniform>();

        _renderRegistry.RegisterFrameBuffer<ScenePassTag, ScenePassDrawSlot>(0,
            RegisterFboEntry.MakeMsaa(RenderBufferMsaa.X4).AttachColorTexture().AttachDepthStencilBuffer()
        );
        _renderRegistry.RegisterFrameBuffer<ScenePassTag, ScenePassResolveSlot>(0,
            RegisterFboEntry.MakeDefault(true).AttachColorTexture().AttachDepthStencilBuffer()
        );
        _renderRegistry.RegisterFrameBuffer<PostPassTag, PostPassASlot>(0,
            RegisterFboEntry.MakePost(true).AttachColorTexture()
        );
        _renderRegistry.RegisterFrameBuffer<PostPassTag, PostPassBSlot>(0,
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );

        foreach (var shader in shaders)
        {
            _renderRegistry.RegisterShader(shader.ResourceId);
        }

        _renderRegistry.FinishRegistration();
    }

    internal void Initialize(MaterialStore materialStore, AssetSystem assets)
    {
        _materialStore = materialStore;
        _drawProcessor = new DrawProcessor(_gfx, _materialStore, _renderRegistry);
        _drawUniforms = new DrawUniforms((Camera3D)Camera, _gfx.Buffers, _renderRegistry);
        _commandCollector = new DrawCommandCollector();
        _pipeline = new RenderPipeline(_drawProcessor);

        _batches.Register(new TerrainBatcher(_gfx));
        _batches.Register(new SpriteBatcher(_gfx));
        _batches.Register(new TilemapBatcher(_gfx, 64, 32));


        // Collector
        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);


        _cmdProducerCtx = new CommandProducerContext { Gfx = _gfx, DrawBatchers = _batches, };
        _commandCollector.AttachContext(_cmdProducerCtx);
        _pipeline.Initialize();
        _commandCollector.InitializeProducers();
        _drawProcessor.Initialize();

        InitializeRenderPasses(assets);
        _initialized = true;
    }

    private void InitializeRenderPasses(AssetSystem assets)
    {
        _renderPassRegistry = new RenderPassRegistry(new RenderCommandOps(_gfx, _drawProcessor));

        var compositeShader = assets.Get<Shader>("Composite").ResourceId;
        var presentShader = assets.Get<Shader>("Present").ResourceId;
        var colorFilterShader = assets.Get<Shader>("ColorFilter").ResourceId;

        // Scene Target
        // Pass 0: draw scene into MSAA FBO
        _renderPassRegistry.Register<ScenePassTag, ScenePassDrawSlot>(RenderTargetId.Scene, PassOpKind.Normal, 0,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue),
                    PassState = GfxPassState.MakeScene(),
                    Samples = 4
                })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, state.PassState);
                ctx.MutateStatePass<ScenePassTag, ScenePassResolveSlot>(PassOpKind.Resolve,
                    PassMutationState.MakeTargetMut(ctx.FboId));
                return ApplyPassReturn.NormalPassResult();
            });

        _renderPassRegistry
            .Register<ScenePassTag, ScenePassResolveSlot>(RenderTargetId.Scene, PassOpKind.Resolve, 1,
                new RenderPassState { PassState = GfxPassState.MakeOff(), LinearFilter = false })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.CmdOps.ToggleStates(new GfxPassState { FramebufferSrgb = false });
                ctx.CmdOps.Blit(state.TargetFboId, ctx.FboId, false);

                var texId = ctx.Meta.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag, PostPassASlot>(PassOpKind.Fsq, 0, texId);
                return ApplyPassReturn.ResolveTargetResult();
            });

        // Post A
        _renderPassRegistry.Register<PostPassTag, PostPassASlot>(RenderTargetId.PostEffect, PassOpKind.Fsq, 0,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
                    PassState = GfxPassState.MakePostProcess(),
                    ShaderId = compositeShader
                })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, state.PassState);
                ctx.CmdOps.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                _gfxCmd.EndRenderPass();
                var texId = ctx.Meta.Attachments.ColorTextureId;
                ctx.CmdOps.GenerateMips(texId);
                ctx.SampleTo<PostPassTag, PostPassBSlot>(PassOpKind.Fsq, 0, texId);
            });

        // Post B
        _renderPassRegistry.Register<PostPassTag, PostPassBSlot>(RenderTargetId.PostEffect, PassOpKind.Fsq, 1,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
                    PassState = GfxPassState.MakePostProcess(),
                    ShaderId = colorFilterShader
                })
            .WithFbo(1)
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, state.PassState);
                ctx.CmdOps.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Meta.Attachments.ColorTextureId;
                ctx.SampleTo<ScreenPassTag, ScreenPassPresentSlot>(PassOpKind.Screen, 0, texId);
                _gfxCmd.EndRenderPass();
            });

        // Screen
        _renderPassRegistry.Register<ScreenPassTag, ScreenPassPresentSlot>(RenderTargetId.Screen, PassOpKind.Screen, 0,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
                    PassState = GfxPassState.MakeScreen(),
                    ShaderId = presentShader
                })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();

                ctx.CmdOps.BeginScreenPass(state.ClearColor, state.PassState);
                ctx.CmdOps.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.ScreenPassResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) => { });
    }


    internal void RegisterScene(in Vector2D<int> outputSize, RenderType renderType, RenderTargetDescriptor desc)
    {
        if (!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");

        SceneRenderProps.Commit();
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId)
    {
        return;
    }
    //_render.MutateRenderPass(targetId);


    internal void BeginTick(in UpdateInfo update) => _commandCollector.BeginTick(update);
    internal void EndTick() => _commandCollector.EndTick();

    internal void Render(float alpha, in FrameInfo frameCtx)
    {
        Debug.Assert(_initialized);
        _frameCtx = frameCtx;
        if (frameCtx.Viewport != Camera.Viewport)
            Camera.Viewport = frameCtx.Viewport;

        SceneRenderProps.SetOutputSize(frameCtx.OutputSize.ToSize2D());
        _snapshot = SceneRenderProps.Commit();

        PrepareRenderer(alpha);
        Execute(alpha, in frameCtx);
        _pipeline.Reset();
    }

    private void PrepareRenderer(float alpha)
    {
        //_render.Prepare(alpha, in frameCtx, in _snapshot);

        _renderPassRegistry.Prepare();
        _sceneDrawProducer.SetSceneGlobals(in _snapshot);
        _commandCollector.Collect(alpha, in _snapshot, _pipeline);
        _pipeline.Prepare();
    }

    private void Execute(float alpha, in FrameInfo frameCtx)
    {
        var capacity =
            UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_pipeline.Count + 100);

        _drawProcessor.Prepare(in _snapshot, capacity);
        _drawUniforms.UploadGlobalUniforms(alpha, in frameCtx, in _snapshot);

        _pipeline.DrainTransformQueue();

        var renderPasses = _renderPassRegistry.RenderPasses;
        foreach (var pass in renderPasses)
        {
            ExecutePass(in pass);
        }
    }

    private void ExecutePass(in RenderPassEntry pass)
    {
        Debug.Assert(pass != null);
        var passCtx = _renderPassRegistry.Ctx;

        if (_renderRegistry.TryGetRenderFbo(pass.TagKey.ToFboTagKey(0), out var fbo))
            passCtx.AttachPass(fbo, pass.PassIndex, pass.TagKey);
        else if (pass.TagKey.PassOp == PassOpKind.Screen)
            passCtx.AttachScreenPass(_snapshot.OutputSize, pass.PassIndex, pass.TagKey);
        else
            return;


        var passResult = pass.ApplyPass(in passCtx);
        if (passResult.OpKind == PassOpKind.Resolve)
            return;

        if (pass.TargetId == RenderTargetId.Scene && pass.PassIndex == 0)
            _pipeline.DrainCommandQueue(RenderTargetId.Scene);

        pass.ApplyAfterPass(in passCtx);

/*
        var key = (pass.TargetId, pass.Index);
        if (_renderPassRegistry.FsqBindings.TryGetValue(key, out var sources) && sources.Count > 0)
        {
            _drawProcessor.DrawFullscreenQuad(passCtx.ScreenShader, sources);
        }
*/

/*
        if (pass is BlitRenderPass blitPass)
        {
            _gfxCmd.BlitFramebuffer(blitPass.BlitFbo, blitPass.TargetFbo, blitPass.LinearFilter);
            return;
        }

        var isScreenPass = pass.TargetFbo == default;

        //if(pass.DepthTest) _gfxCmd.SetDepthMode(DepthMode.Lequal);

        var isScenePass = pass is IScenePass;

        if (pass.TargetFbo == default)
            _gfxCmd.BeginScreenPass(pass.Clear);
        else
            _gfxCmd.BeginRenderPass(pass.TargetFbo, pass.Clear, isScenePass);


        switch (pass)
        {
            case IScenePass scePass:
                _render.RenderScenePass(scePass, _commandSubmitter);
                break;
            case PostEffectPass pPass:
                _render.RenderPostEffectPass(pPass);
                break;
            case ScreenPass scrPass:
                _render.RenderScreenPass(scrPass);
                break;
            case IDepthPass dPass:
                _render.RenderDepthPass(dPass, _commandSubmitter);
                break;
        }

        if (pass.Op == RenderPassOp.DrawScene)
        {
            _gfxCmd.EndRenderPass();
            return;
        }

        if (!isScreenPass)
            _gfxCmd.EndRenderPass();

        if (pass is PostEffectPass postEffectPass && postEffectPass.GenerateMipMapAfter)
        {
            _gfx.Textures.GenerateMipMaps(postEffectPass.OutputTexture);
        }
        */
    }

    public void Shutdown()
    {
    }
}