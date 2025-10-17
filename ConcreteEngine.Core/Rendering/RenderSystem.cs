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
    //public static RenderSetupBuilder StartBuilder() => new RenderSetupBuilder();

    private readonly GraphicsRuntime _graphics;
    private readonly GfxContext _gfx;

    private RenderRegistry _renderRegistry;

    private DrawCommandPipeline _drawPipeline;
    private RenderPassPipeline _passPipeline;

    private readonly BatcherRegistry _batches = new();

    public RenderSceneProps RenderProps { get; }
    private RenderSceneState _snapshot;

    private readonly RenderView _renderView = new();

    private bool _initialized = false;

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

    internal void InitializeRegistry(ReadOnlySpan<ShaderId> shaderIds, in RenderCoreShaders coreShaders)
    {
        _renderRegistry = new RenderRegistry(_gfx);
        _renderRegistry.BeginRegistration(_initialSize);
        _renderRegistry.ShaderRegistry.RegisterCollection(shaderIds);
        _renderRegistry.ShaderRegistry .RegisterCoreShader(in coreShaders);
        _renderRegistry.FinishRegistration();
    }

    internal void InitializeDraw()
    {
        _batches.Register(new TerrainBatcher(_gfx));
        //_batches.Register(new SpriteBatcher(_gfx));
        //_batches.Register(new TilemapBatcher(_gfx, 64, 32));

        _drawPipeline = new DrawCommandPipeline(_gfx, _batches, _renderRegistry, _renderView, _snapshot);
        _drawPipeline.Initialize();

        RegisterPasses();
        _initialized = true;
    }

    private void RegisterPasses()
    {
        _passPipeline = new RenderPassPipeline(_drawPipeline.DrawStateOps, _renderRegistry);
        PassPipeline3D.RegisterPassPipeline(_passPipeline, in _renderRegistry.ShaderRegistry.CoreShaders);
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
        _drawPipeline.Buffers.UploadGlobalUniforms(in frameInfo, in runtimeParams);
        _drawPipeline.Buffers.UploadCameraView(_renderView);
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