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
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;

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

    private MaterialStore _materialStore = null!;

    private readonly BatcherRegistry _batches = new();

    public SceneRenderProperties SceneRenderProps { get; }
    private RenderGlobalSnapshot _snapshot;

    public ICamera Camera { get; } = new Camera3D();
    private readonly RenderView _renderView = new();

    private bool _initialized = false;
    private GfxFrameInfo _frameCtx;


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
        RenderStaticSetup.RegisterUniformBufferTypes(_renderRegistry);
        RenderStaticSetup.RegisterFrameBuffers(_renderRegistry);
        _renderRegistry.RegisterShaderCollection(shaders);
        _renderRegistry.FinishRegistration();
    }

    internal void Initialize(MaterialStore materialStore, AssetSystem assets)
    {
        _materialStore = materialStore;
        _drawProcessor = new DrawProcessor(_gfx, _materialStore, _renderRegistry);
        _drawUniforms = new DrawUniforms(_gfx.Buffers, _renderRegistry);
        _pipelineStateOps = new PipelineStateOps(_gfx, _renderRegistry, _renderView, _snapshot, _drawUniforms);

        _batches.Register(new TerrainBatcher(_gfx));
        //_batches.Register(new SpriteBatcher(_gfx));
        //_batches.Register(new TilemapBatcher(_gfx, 64, 32));


        _drawPipeline = new DrawCommandPipeline();
        _drawPipeline.Initialize(_gfx, _batches, _drawProcessor);

        _drawProcessor.Initialize();

        RegisterPasses(assets);
        _initialized = true;
    }

    private ShaderId depthShader;

    private void RegisterPasses(AssetSystem assets)
    {
        _passPipeline = new RenderPassPipeline(_pipelineStateOps, _renderRegistry);

        var compositeShader = assets.Get<Shader>("Composite").ResourceId;
        var presentShader = assets.Get<Shader>("Present").ResourceId;
        var colorFilterShader = assets.Get<Shader>("ColorFilter").ResourceId;
        depthShader = assets.Get<Shader>("Depth").ResourceId;

        // Shadow
        _passPipeline.Register<ShadowPassTag, PassDrawSlot>(PassOpKind.Draw, 0, RenderPassState.MakeShadow())
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.UseRenderLightView(); // Note!
                
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(new GfxPassStateFunc(Cull: CullMode.FrontCcw));
                return ApplyPassReturn.DrawPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.EndRenderPass();
                ctx.Ops.RestoreView();
            });

        // Scene 
        // Pass 0: draw scene into MSAA FBO
        _passPipeline.Register<ScenePassTag, PassDrawSlot>(PassOpKind.Draw, 0,
                RenderPassState.MakeSceneMsaa(4))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(new GfxPassStateFunc(Cull: CullMode.BackCcw));

                ctx.MutateStatePass<ScenePassTag, PassResolveSlot>(
                    PassOpKind.Resolve,
                    PassMutationState.MutateTarget(ctx.Target.FboId)
                );
                return ApplyPassReturn.DrawPassResult();
            });

        // Pass 1: resolve to scene FBO
        _passPipeline.Register<ScenePassTag, PassResolveSlot>(PassOpKind.Resolve, 1,
                RenderPassState.MakeResolve())
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.ToggleStates(new GfxPassState { FramebufferSrgb = false });
                ctx.Ops.Blit(state.TargetFboId, ctx.Target.FboId, state.LinearFilter);
                return ApplyPassReturn.ResolveTargetResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag, PassPostASlot>(PassOpKind.Fsq, 0, texId);

                ctx.Ops.EndRenderPass();
                ctx.Ops.GenerateMips(texId);
            });

        // Post A
        _passPipeline.Register<PostPassTag, PassPostASlot>(PassOpKind.Fsq, 0,
                RenderPassState.MakePostProcess(compositeShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<PostPassTag, PassPostBSlot>(PassOpKind.Fsq, 0, texId);

                ctx.Ops.EndRenderPass();
            });

        // Post B
        _passPipeline.Register<PostPassTag, PassPostBSlot>(PassOpKind.Fsq, 1,
                RenderPassState.MakePostProcess(colorFilterShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.FsqPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var texId = ctx.Target.Attachments.ColorTextureId;
                ctx.SampleTo<ScreenPassTag, PassFinalSlot>(PassOpKind.Screen, 0, texId);

                ctx.Ops.EndRenderPass();
            });

        // Screen
        _passPipeline.Register<ScreenPassTag, PassFinalSlot>(PassOpKind.Screen, 0,
                RenderPassState.MakeScreen(presentShader))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                var sources = ctx.GetPassSources();

                ctx.Ops.BeginScreenPass(state.ClearColor, state.PassState);
                ctx.Ops.DrawFullscreenQuad(state.ShaderId, sources);
                return ApplyPassReturn.ScreenPassResult();
            });
    }


    internal void RegisterScene(RenderType renderType, RenderTargetDescriptor desc)
    {
        if (!_initialized)
            throw new InvalidOperationException("Renderer is not initialized");

        SceneRenderProps.Commit();
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _drawPipeline.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);


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

        _renderView.PrepareFrame((Camera3D)Camera);

        _drawUniforms.UploadGlobalUniforms(alpha, in frameCtx, _snapshot);
        _drawUniforms.UploadCameraView(_renderView);

        Execute(alpha, in frameCtx);
    }


    private void Execute(float alpha, in GfxFrameInfo frameCtx)
    {
        _passPipeline.Prepare(frameCtx.OutputSize);

        nint capacity = _drawPipeline.Prepare(alpha, _snapshot);

        _drawProcessor.PrepareFrame(in _snapshot, capacity);

        _drawPipeline.ExecuteTransforms();

        while (_passPipeline.NextPass(out var nextPassRes))
        {
            if (nextPassRes.SkipPass) continue;
            ExecutePass(nextPassRes.PassId);
        }
    }

    private void ExecutePass(PassId passId)
    {
        var passResult = _passPipeline.ApplyPass();

        if (passResult.OpKind == PassOpKind.Resolve)
        {
            _passPipeline.ApplyAfterPass();
            return;
        }

        if (passResult == ApplyPassReturn.DrawPassResult())
        {
            _drawProcessor.PrepareDrawPass(passId == 0 ? depthShader : default);
            _drawPipeline.ExecuteDrawPass(passId);
        }

        _passPipeline.ApplyAfterPass();
        
    }

    public void Shutdown()
    {
    }
}