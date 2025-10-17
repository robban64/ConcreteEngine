#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Resources;
using MaterialStore = ConcreteEngine.Core.Assets.Materials.MaterialStore;
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
    RenderSceneProps RenderProps { get; }
    TSink GetSink<TSink>() where TSink : IDrawSink;
}

public sealed class RenderSystem : IRenderSystem
{
    private readonly GraphicsRuntime _graphics;
    private readonly GfxContext _gfx;

    private RenderRegistry _renderRegistry;

    private DrawCommandPipeline _drawPipeline;
    private RenderPassPipeline _passPipeline;

    private DrawStateOps _drawStateOps = null!;
    private DrawCommandProcessor _cmdDraw = null!;
    private DrawBuffers _drawBuffers = null!;

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

    internal void InitializeGraphics(ReadOnlySpan<ShaderId> shaderIds)
    {
        InvalidOpThrower.ThrowIf(_initialSize.Width <= 1);
        InvalidOpThrower.ThrowIf(_initialSize.Height <= 1);

        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_initialSize);
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds);
        _renderRegistry.FinishRegistration();
    }

    internal void Initialize(AssetSystem assets)
    {
        var depthShader = assets.Store.GetByName<Shader>("Depth").ResourceId;
        var depthKey = TagRegistry.FboKey<ShadowPassTag>(FboVariant.Default);

        _renderRegistry.TryGetRenderFbo(depthKey, out var shadowFbo);

        InvalidOpThrower.ThrowIfNull(shadowFbo, nameof(shadowFbo));
        InvalidOpThrower.ThrowIfNot(shadowFbo.Attachments.DepthTextureId.IsValid());


        var drawCtx = new DrawStateContext(depthShader, shadowFbo!.Attachments.DepthTextureId);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = _gfx, Registry = _renderRegistry, RenderView = _renderView, Snapshot = _snapshot
        };

        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _cmdDraw = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        _batches.Register(new TerrainBatcher(_gfx));
        //_batches.Register(new SpriteBatcher(_gfx));
        //_batches.Register(new TilemapBatcher(_gfx, 64, 32));


        _drawPipeline = new DrawCommandPipeline();
        _drawPipeline.Initialize(_gfx, _batches, _cmdDraw, _drawBuffers);

        _cmdDraw.Initialize();
        _drawBuffers.AttachMaterialBuffer(_drawPipeline.MaterialBuffer);

        RegisterPasses(assets.Store);
        _initialized = true;
    }


    private void RegisterPasses(IAssetStore assets)
    {
        _passPipeline = new RenderPassPipeline(_drawStateOps, _renderRegistry);

        var compositeShader = assets.GetByName<Shader>("Composite").ResourceId;
        var presentShader = assets.GetByName<Shader>("Present").ResourceId;
        var colorFilterShader = assets.GetByName<Shader>("ColorFilter").ResourceId;

        TempPassSetup.RegisterPassPipeline(_passPipeline, compositeShader, presentShader, colorFilterShader);
    }

    public TSink GetSink<TSink>() where TSink : IDrawSink => _drawPipeline.GetSink<TSink>();

    internal void BeginTick(in UpdateTickInfo tick) => _drawPipeline.BeginTick(tick);
    internal void EndTick() => _drawPipeline.EndTick();



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
            _renderRegistry.FboRegistry.RecreateSizedFrameBuffer(frameInfo.OutputSize);
        }

        _graphics.BeginFrame(frameInfo.ToGfxFrameInfo());

        _snapshot = RenderProps.Commit();
        _renderView.PrepareFrame(in viewSnapshot);
        _drawBuffers.UploadGlobalUniforms(in frameInfo, in runtimeParams);
        _drawBuffers.UploadCameraView(_renderView);
    }

    internal void EndRenderFrame(out GfxFrameResult frameResult)
    {
        _graphics.EndFrame(out frameResult);
    }
    public void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _drawPipeline.SubmitMaterialDrawData(in payload, slots);

    internal void Render(in RenderFrameInfo frameInfo, Action uploadMaterialDel)
    {
        Debug.Assert(_initialized);

        _passPipeline.Prepare(frameInfo.OutputSize);
        var (drawCapacity, matCapacity) = _drawPipeline.Prepare(frameInfo.Alpha, _snapshot);
        uploadMaterialDel();
        
        _cmdDraw.PrepareFrame(drawCapacity, matCapacity);

        _drawPipeline.ExecuteMaterials();
        _drawPipeline.ExecuteTransforms();

        while (_passPipeline.NextPass(out var nextPassRes))
        {
            if (nextPassRes.ActionKind == PreparePassActionKind.Skip) continue;
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

        if (passResult == PassAction.DrawPassResult())
        {
            _cmdDraw.PrepareDrawPass();
            _drawPipeline.ExecuteDrawPass(passId);
        }

        _passPipeline.ApplyAfterPass();
    }

    public void Shutdown()
    {
    }
}