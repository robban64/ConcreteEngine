#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Resources;
using RenderFrameInfo = ConcreteEngine.Core.Rendering.State.RenderFrameInfo;

#endregion

namespace ConcreteEngine.Core.Rendering;

public enum RenderType
{
    Render2D,
    Render3D
}

public interface IRenderSystem : IGameEngineSystem
{
    TSink GetSink<TSink>() where TSink : IDrawSink;
    Material CreateMaterial(string templateName);

    RenderSceneProps RenderProps { get; }
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly GraphicsRuntime _graphics;
    private readonly GfxContext _gfx;

    private RenderRegistry _renderRegistry;

    private DrawCommandPipeline _drawPipeline;
    private RenderPassPipeline _passPipeline;

    private DrawStateOps _drawStateOps = null!;
    private DrawProcessor _drawProcessor = null!;
    private DrawUniforms _drawUniforms = null!;

    private MaterialStore _materialStore = null!;

    private readonly BatcherRegistry _batches = new();

    public RenderSceneProps RenderProps { get; }
    private RenderSceneState _snapshot;

    private readonly RenderView _renderView = new();

    private bool _initialized = false;

    internal RenderRegistry RenderRegistry => _renderRegistry;

    private Size2D _initialSize;

    internal RenderSystem(GraphicsRuntime graphics, Size2D outputSize)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        RenderProps = new RenderSceneProps();
        RenderProps.Commit();
        _snapshot = RenderProps.Snapshot;
        _initialSize = outputSize;
    }

    internal void InitializeGraphics(IReadOnlyList<Shader> shaders)
    {
        InvalidOpThrower.ThrowIf(_initialSize.Width <= 1);
        InvalidOpThrower.ThrowIf(_initialSize.Height <= 1);

        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_initialSize);
        RenderStaticSetup.RegisterUniformBufferTypes(_renderRegistry);
        RenderStaticSetup.RegisterFrameBuffers(_renderRegistry);
        _renderRegistry.RegisterShaderCollection(shaders);
        _renderRegistry.FinishRegistration();
    }

    internal void Initialize(MaterialStore materialStore, AssetSystem assets)
    {
        var depthShader = assets.Get<Shader>("Depth").ResourceId;
        _renderRegistry.TryGetRenderFbo<ShadowPassTag, PassDrawSlot>(out var shadowFbo);
        InvalidOpThrower.ThrowIfNull(shadowFbo, nameof(shadowFbo));
        InvalidOpThrower.ThrowIfNot(shadowFbo.Attachments.DepthTextureId.IsValid());

        _materialStore = materialStore;

        var drawCtx = new DrawStateContext(depthShader, shadowFbo!.Attachments.DepthTextureId);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = _gfx, Registry = _renderRegistry, RenderView = _renderView, Snapshot = _snapshot
        };

        _drawUniforms = new DrawUniforms(_gfx.Buffers, _renderRegistry, _snapshot);
        _drawProcessor = new DrawProcessor(drawCtx, drawCtxPayload, _materialStore);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawUniforms);

        _batches.Register(new TerrainBatcher(_gfx));
        //_batches.Register(new SpriteBatcher(_gfx));
        //_batches.Register(new TilemapBatcher(_gfx, 64, 32));


        _drawPipeline = new DrawCommandPipeline();
        _drawPipeline.Initialize(_gfx, _batches, _drawProcessor);

        _drawProcessor.Initialize();

        RegisterPasses(assets);
        _initialized = true;
    }


    private void RegisterPasses(AssetSystem assets)
    {
        _passPipeline = new RenderPassPipeline(_drawStateOps, _renderRegistry);

        var compositeShader = assets.Get<Shader>("Composite").ResourceId;
        var presentShader = assets.Get<Shader>("Present").ResourceId;
        var colorFilterShader = assets.Get<Shader>("ColorFilter").ResourceId;

        // Shadow
        _passPipeline.Register<ShadowPassTag, PassDrawSlot>(PassOpKind.Draw, 0, RenderPassState.MakeShadow())
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.ActivateDepthMode(); // Note!

                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDepth());
                return ApplyPassReturn.DrawPassResult();
            }).OnPassEnd(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.EndRenderPass();
                ctx.Ops.RestoreMode();
            });

        // Scene 
        // Pass 0: draw scene into MSAA FBO
        _passPipeline.Register<ScenePassTag>(PassOpKind.Draw, 0,
                RenderPassState.MakeSceneMsaa(4))
            .OnPassBegin(static (RenderPassCtx ctx, in RenderPassState state) =>
            {
                ctx.Ops.BeginRenderPass(ctx.Target.FboId, state.ClearColor, state.PassState);
                ctx.Ops.ApplyStateFunctions(GfxPassStateFunc.MakeDefault());

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

        RenderProps.Commit();
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _drawPipeline.GetSink<TSink>();

    public Material CreateMaterial(string templateName) => _materialStore.CreateMaterialFromTemplate(templateName);

    internal void BeginTick(in UpdateTickInfo tick) => _drawPipeline.BeginTick(tick);
    internal void EndTick() => _drawPipeline.EndTick();

    private void RecreateFbo(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Height, 1, nameof(outputSize));

        var fbos = RenderRegistry.RenderFbos;
        Span<(FrameBufferId, Size2D)> newSizes = stackalloc (FrameBufferId, Size2D)[fbos.Count];
        var idx = 0;
        foreach (var fbo in fbos)
        {
            if (fbo.IsFixedSize) continue;
            newSizes[idx++] = (fbo.FboId, fbo.CalculateNewSize(outputSize));
        }

        _graphics.RecreateFbo(newSizes.Slice(0, idx));
    }

    //
    internal void RenderEmptyFrame(in RenderFrameInfo frameInfo)
    {
        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());
        _graphics.EndFrame(out _);
    }

    internal void BeginRenderFrame(
        BeginFrameStatus status,
        in RenderFrameInfo frameInfo,
        in RenderRuntimeParams runtimeParams,
        in RenderViewSnapshot viewSnapshot
    )
    {
        Debug.Assert(_initialized);

        if (status == BeginFrameStatus.Resize)
        {
            RecreateFbo(frameInfo.OutputSize);
        }

        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());

        _snapshot = RenderProps.Commit();
        _renderView.PrepareFrame(in viewSnapshot);
        _drawUniforms.UploadGlobalUniforms(in frameInfo, in runtimeParams);
        _drawUniforms.UploadCameraView(_renderView);
    }

    internal void EndRenderFrame(out GfxFrameResult frameResult)
    {
        _graphics.EndFrame(out frameResult);
    }

    internal void Render(in RenderFrameInfo frameInfo)
    {
        Debug.Assert(_initialized);

        _passPipeline.Prepare(frameInfo.OutputSize);

        nint capacity = _drawPipeline.Prepare(frameInfo.Alpha, _snapshot);

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
            _drawProcessor.PrepareDrawPass();
            _drawPipeline.ExecuteDrawPass(passId);
        }

        _passPipeline.ApplyAfterPass();
    }

    public void Shutdown()
    {
    }
}