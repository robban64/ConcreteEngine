#region

using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
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

    private readonly RenderRegistry _renderRegistry;
    private readonly DrawCommandPipeline _drawPipeline;
    private readonly RenderPassPipeline _passPipeline;

    private readonly BatcherRegistry _batches = new();

    public RenderSceneProps RenderProps { get; }
    private RenderSceneState _snapshot;

    private readonly RenderView _renderView;
    
    private RenderSystemContext SystemContext { get; }

    private bool _initialized = false;

    private Size2D _initialSize;

    internal RenderSystem(GraphicsRuntime graphics, Size2D outputSize)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;

        _renderView = new RenderView();

        RenderProps = new RenderSceneProps();
        RenderProps.Commit();
        _snapshot = RenderProps.Snapshot;
        _initialSize = outputSize;

        _renderRegistry = new RenderRegistry(_gfx);
        _drawPipeline = new DrawCommandPipeline();
        _passPipeline = new RenderPassPipeline();

        SystemContext = new RenderSystemContext
        {
            Batchers = _batches,
            CommandPipeline = _drawPipeline,
            Gfx = _gfx,
            Registry = _renderRegistry,
            PassPipeline = _passPipeline,
            Snapshot = _snapshot,
            View = _renderView
        };
    }

    public RenderSetupBuilder StartBuilder() => new (SystemContext, _initialSize);

    public void ApplyBuilder(RenderSetupBuilder builder)
    {
        InvalidOpThrower.ThrowIf(builder.IsDone, nameof(builder.IsDone));

        var plan = builder.Build();
        
        _renderRegistry.BeginRegistration(_initialSize);
        plan.FboSetup(_renderRegistry.FboRegistry);
        
        var shaderIds = plan.ShaderProvider();
        var coreShaders = plan.CoreShaderSetup();
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds);
        _renderRegistry.ShaderRegistry.RegisterCoreShader(in coreShaders);
        _renderRegistry.FinishRegistration();

        plan.BatcherSetup(_gfx, _batches);
        
        var ctx = new RenderSystemContext
        {
            Batchers = _batches,
            CommandPipeline = _drawPipeline,
            Gfx = _gfx,
            Registry = _renderRegistry,
            PassPipeline = _passPipeline,
            Snapshot = _snapshot,
            View = _renderView
        };
        _drawPipeline.Initialize(ctx, plan.CollectorSetup);
        _passPipeline.Initialize(ctx);

        PassPipeline3D.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
        _initialized = true;

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
        _drawPipeline.DrawBuffer.UploadGlobalUniforms(in frameInfo, in runtimeParams);
        _drawPipeline.DrawBuffer.UploadCameraView(_renderView);
    }

    internal void EndRenderFrame(out GfxFrameResult frameResult)
    {
        _graphics.EndFrame(out frameResult);
    }

    public void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _drawPipeline.SubmitMaterialDrawData(in payload, slots);

    //TODO remove the temp delegate
    internal void Render(in RenderFrameInfo frameInfo, Action uploadMaterialDel)
    {
        Debug.Assert(_initialized);

        _passPipeline.Prepare(frameInfo.OutputSize);
        var (drawCapacity, matCapacity) = _drawPipeline.Prepare(frameInfo.Alpha, _snapshot);
        uploadMaterialDel();

        _drawPipeline.DrawCmdProcessor.PrepareFrame(drawCapacity, matCapacity);

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
            _drawPipeline.DrawCmdProcessor.PrepareDrawPass();
            _drawPipeline.ExecuteDrawPass(passId);
        }

        _passPipeline.ApplyAfterPass();
    }

    public void Shutdown()
    {
    }
}