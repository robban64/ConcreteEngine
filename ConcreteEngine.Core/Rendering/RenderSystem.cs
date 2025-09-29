#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
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

        _renderRegistry.RegisterFrameBuffer(RenderTargetId.Scene,
            RegisterFboEntry.MakeMsaa(RenderBufferMsaa.X4).AttachColorTexture().AttachDepthStencilBuffer()
        );
        _renderRegistry.RegisterFrameBuffer(RenderTargetId.Scene,
            RegisterFboEntry.MakeDefault().AttachColorTexture().AttachDepthStencilBuffer()
        );

        _renderRegistry.RegisterFrameBuffer(RenderTargetId.PostEffect,
            RegisterFboEntry.MakePost(true).AttachColorTexture()
        );
        _renderRegistry.RegisterFrameBuffer(RenderTargetId.PostEffect,
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );

        foreach (var shader in shaders)
        {
            _renderRegistry.RegisterShader(shader.ResourceId);
        }

        _renderRegistry.FinishRegistration();


        _renderPassRegistry = new RenderPassRegistry(new RenderCommandOps(_gfx, _drawProcessor));

        // Scene Target
        // Pass 0: draw scene into MSAA FBO
        _renderPassRegistry.Register(RenderTargetId.Scene, new ScenePassState(default))
            .WithFbo(0)
            .AddBeforeOp((in RenderPassCtx ctx, in ScenePassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, true);
                return PassReturn.ResolveTo(RenderTargetId.Scene, 1, 0, ctx.FboId);
            });

        _renderPassRegistry.Register(RenderTargetId.Scene, new ResolvePassState())
            .WithFbo(1)
            .AddBeforeOp((in RenderPassCtx ctx, in ResolvePassState state) =>
            {
                var texId = ctx.Meta.Attachments.ColorTextureId;
                // TODO
                ctx.CmdOps.Blit(ctx.FboId, state.BlitFbo, true);
                return PassReturn.SampleIn(RenderTargetId.PostEffect, 0, 0, texId);
            });
        //ApplyState(_activeState with{ FramebufferSrgb = false });

        _renderPassRegistry.Register(RenderTargetId.PostEffect, new PostPassState())
            .WithFbo(0)
            .AddBeforeOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, false);
                //ctx.CmdOps.UpdateStates(state.PassState);
                //ctx.CmdOps.ClearColor(state.ClearColor);

                return PassReturn.None;
            }).AddAfterOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                var texId = ctx.Meta.Attachments.ColorTextureId;
                if (ctx.Pass == 0) ctx.CmdOps.GenerateMips(texId);
                return PassReturn.SampleIn(RenderTargetId.PostEffect, 1, 0, texId);
            });

        _renderPassRegistry.Register(RenderTargetId.PostEffect, new PostPassState())
            .WithFbo(1)
            .AddBeforeOp((in RenderPassCtx ctx, in PostPassState state) =>
            {
                var texId = ctx.Meta.Attachments.ColorTextureId;
                ctx.CmdOps.BeginRenderPass(ctx.FboId, state.ClearColor, false);
                return PassReturn.SampleIn(RenderTargetId.Screen, 0, 0, texId);

                //ctx.CmdOps.UpdateStates(state.PassState);
                //ctx.CmdOps.ClearColor(state.ClearColor);
            });

        _renderPassRegistry.Register(RenderTargetId.Screen, new ScreenPassState())
            .AddBeforeOp((in RenderPassCtx ctx, in ScreenPassState state) =>
            {
                ctx.CmdOps.BeginScreenPass(state.ClearColor);
                return PassReturn.None;
                //...
            });
    }

    internal void Initialize(MaterialStore materialStore)
    {
        _materialStore = materialStore;
        _drawProcessor = new DrawProcessor(_gfx, _materialStore, _renderRegistry);
        _drawUniforms = new DrawUniforms((Camera3D)Camera, _gfx.Buffers, _renderRegistry);
        _commandCollector = new DrawCommandCollector();
        _pipeline = new RenderPipeline(_drawProcessor);

        _batches.Register(new TerrainBatcher(_gfx));
        _batches.Register(new SpriteBatcher(_gfx));
        _batches.Register(new TilemapBatcher(_gfx, 64, 32));

        _cmdProducerCtx = new CommandProducerContext { Gfx = _gfx, DrawBatchers = _batches, };

        // Collector
        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);


        _commandCollector.AttachContext(_cmdProducerCtx);
        _pipeline.Initialize();
        _commandCollector.InitializeProducers();
        _drawProcessor.Initialize();

        _initialized = true;
    }


    internal void RegisterScene(in Vector2D<int> outputSize, RenderType renderType, RenderTargetDescriptor desc)
    {
        if (!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");

        SceneRenderProps.Commit();
        //_render.RegisterRenderTargetsFrom(in outputSize, desc);
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
        SceneRenderProps.Commit();

        _snapshot = SceneRenderProps.CurrentSnapshot;

        PrepareRenderer(alpha);
        Execute(alpha, in frameCtx);
        _pipeline.Reset();
    }

    private void PrepareRenderer(float alpha)
    {
        //_render.Prepare(alpha, in frameCtx, in _snapshot);

        _renderPassRegistry.ResetFrame();
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

    private void ExecutePass(in IRenderPassEntry pass)
    {
        Debug.Assert(pass != null);
        if (!_renderRegistry.TryGetRenderFbo(pass.TargetId, pass.FboSlot, out var fbo))
            return;

        var passCtx = _renderPassRegistry.Ctx;
        passCtx.Pass = pass.Index;
        passCtx.FromBinding(fbo);

        var beforeResult = pass.ExecuteBefore(in passCtx);

        if (pass.TargetId == RenderTargetId.Scene && pass.Index == 0)
            _pipeline.DrainCommandQueue(RenderTargetId.Scene);

        var afterResult = pass.ExecuteAfter(in passCtx);
        var result = _renderPassRegistry.Collect(in beforeResult, in afterResult);
        // DrawFullscreenQuad

        var key = (pass.TargetId, pass.Index);
        if (_renderPassRegistry.FsqBindings.TryGetValue(key, out var sources) && sources.Count > 0)
        {
            _drawProcessor.DrawFullscreenQuad(passCtx.ScreenShader, sources);
        }

        _gfxCmd.EndRenderPass();

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