#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;

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
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;

    private RenderRegistry _renderRegistry;
    
    private DrawCommandPipeline _drawPipeline;

    private RenderPassPipeline _passPipeline;
    
    private PipelineStateOps _pipelineStateOps = null!;
    private DrawProcessor _drawProcessor = null!;
    private DrawUniforms _drawUniforms = null!;

    private readonly BatcherRegistry _batches = new();

    private MaterialStore _materialStore = null!;

    private bool _initialized = false;
    private GfxFrameInfo _frameCtx;

    private RenderGlobalSnapshot _snapshot;
    
    public SceneRenderProperties SceneRenderProps { get; }

    public ICamera Camera { get; } = new Camera3D();

    internal RenderRegistry RenderRegistry => _renderRegistry;

    internal RenderSystem(IGraphicsRuntime graphics, Size2D outputSize)
    {
        _gfx = graphics.Gfx;
        _gfxCmd = graphics.Gfx.Commands;
        SceneRenderProps = new SceneRenderProperties();
        SceneRenderProps.SetOutputSize(outputSize);
        SceneRenderProps.Commit();
        _snapshot = SceneRenderProps.Snapshot;
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

        _renderRegistry.RegisterFrameBuffer<ScenePassTag, PassDrawSlot>(0,
            RegisterFboEntry.MakeMsaa(RenderBufferMsaa.X4).AttachColorTexture().AttachDepthStencilBuffer()
        );
        _renderRegistry.RegisterFrameBuffer<ScenePassTag, PassResolveSlot>(0,
            RegisterFboEntry.MakeDefault(true).AttachColorTexture().AttachDepthStencilBuffer()
        );
        _renderRegistry.RegisterFrameBuffer<PostPassTag, PassPostASlot>(0,
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );
        _renderRegistry.RegisterFrameBuffer<PostPassTag, PassPostBSlot>(0,
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
        _pipelineStateOps = new PipelineStateOps(_gfx, _drawProcessor, _renderRegistry);

        _batches.Register(new TerrainBatcher(_gfx));
        _batches.Register(new SpriteBatcher(_gfx));
        _batches.Register(new TilemapBatcher(_gfx, 64, 32));


        _drawPipeline = new DrawCommandPipeline();
        _drawPipeline.Initialize(_gfx, _batches, _drawProcessor);

        _drawProcessor.Initialize();

        RegisterPasses(assets);
        _initialized = true;
    }

    private void RegisterPasses(AssetSystem assets)
    {
        _passPipeline = new RenderPassPipeline(_pipelineStateOps, _renderRegistry);

        var compositeShader = assets.Get<Shader>("Composite").ResourceId;
        var presentShader = assets.Get<Shader>("Present").ResourceId;
        var colorFilterShader = assets.Get<Shader>("ColorFilter").ResourceId;

        // Scene Target
        // Pass 0: draw scene into MSAA FBO
        _passPipeline.Register<ScenePassTag, PassDrawSlot>(RenderTargetId.Scene, PassOpKind.Normal, 0,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorDepthClear(Color4.CornflowerBlue),
                    PassState = GfxPassState.MakeScene(),
                    Samples = 4
                })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.CmdOps.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.MutateStatePass<ScenePassTag, PassResolveSlot>(PassOpKind.Resolve,
                    PassMutationState.MakeTargetMut(ctx.Target.FboId));
                return ApplyPassReturn.NormalPassResult();
            });

        _passPipeline
            .Register<ScenePassTag, PassResolveSlot>(RenderTargetId.Scene, PassOpKind.Resolve, 1,
                new RenderPassState { PassState = GfxPassState.MakeOff(), LinearFilter = false })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.CmdOps.ToggleStates(new GfxPassState { FramebufferSrgb = false });
                ctx.CmdOps.Blit(state.TargetFboId, ctx.Target.FboId, false);

                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag, PassPostASlot>(PassOpKind.Fsq, 0, texId);
                return ApplyPassReturn.ResolveTargetResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.CmdOps.EndRenderPass();
                ctx.CmdOps.GenerateMips(texId);
            });

        // Post A
        _passPipeline.Register<PostPassTag, PassPostASlot>(RenderTargetId.PostEffect, PassOpKind.Fsq, 0,
                new RenderPassState
                {
                    ClearColor = GfxPassClear.MakeColorClear(Color4.Black),
                    PassState = GfxPassState.MakePostProcess(),
                    ShaderId = compositeShader
                })
            .OnPassBegin((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.CmdOps.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.CmdOps.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                _gfxCmd.EndRenderPass();
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag, PassPostBSlot>(PassOpKind.Fsq, 0, texId);
            });

        // Post B
        _passPipeline.Register<PostPassTag, PassPostBSlot>(RenderTargetId.PostEffect, PassOpKind.Fsq, 1,
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
                ctx.CmdOps.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.CmdOps.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd((in RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<ScreenPassTag, PassFinalSlot>(PassOpKind.Screen, 0, texId);
                _gfxCmd.EndRenderPass();
            });

        // Screen
        _passPipeline.Register<ScreenPassTag, PassFinalSlot>(RenderTargetId.Screen, PassOpKind.Screen, 0,
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


    internal void RegisterScene(RenderType renderType, RenderTargetDescriptor desc)
    {
        if (!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");

        SceneRenderProps.Commit();
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _drawPipeline.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);

    public void MutateRenderPass(RenderTargetId targetId)
    {
        return;
    }
    //_render.MutateRenderPass(targetId);


    internal void BeginTick(in UpdateInfo update) => _drawPipeline.BeginTick(update);
    internal void EndTick() => _drawPipeline.EndTick();

    internal void Render(float alpha, in GfxFrameInfo frameCtx)
    {
        Debug.Assert(_initialized);
        _frameCtx = frameCtx;
        if (frameCtx.Viewport != Camera.Viewport)
            Camera.Viewport = frameCtx.Viewport;

        SceneRenderProps.SetOutputSize(frameCtx.OutputSize);
        _snapshot = SceneRenderProps.Commit();

        Execute(alpha, in frameCtx);
    }


    private void Execute(float alpha, in GfxFrameInfo frameCtx)
    {
        _passPipeline.Prepare(_snapshot.OutputSize);

        nint capacity = _drawPipeline.Prepare(alpha, _snapshot);
        
        _drawProcessor.Prepare(in _snapshot, capacity);
        _drawUniforms.UploadGlobalUniforms(alpha, in frameCtx, in _snapshot);

        _drawPipeline.ExecuteTransforms();

        while (_passPipeline.NextPass(out var nextPassRes))
        {
            if(nextPassRes.SkipPass) continue;
            ExecutePass(nextPassRes.PassIndex, nextPassRes.TargetId);
        }
    }

    private void ExecutePass(int passIndex, RenderTargetId targetId)
    {
        var passResult = _passPipeline.ApplyPass();
        if (passResult.OpKind == PassOpKind.Resolve)
        {
            _passPipeline.ApplyAfterPass();
            return;
        }

        if (targetId == RenderTargetId.Scene && passIndex == 0)
            _drawPipeline.ExecuteDrawPass(RenderTargetId.Scene);

        _passPipeline.ApplyAfterPass();

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