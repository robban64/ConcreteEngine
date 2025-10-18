#region

using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using RenderFrameInfo = ConcreteEngine.Core.Rendering.State.RenderFrameInfo;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector = null!;
    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _commandBuffer = null!;
    private MaterialDrawBuffer _materialBuffer = null!;

    private DrawCommandProcessor _drawCmdProc = null!;
    private DrawBuffers _drawBuffers = null!;
    private DrawStateOps _drawStateOps = null!;

    private RenderSystemContext _ctx = null!;


    internal DrawStateOps DrawStateOps => _drawStateOps;
    internal DrawBuffers DrawBuffer => _drawBuffers;
    internal DrawCommandProcessor DrawCmdProcessor => _drawCmdProc;
    internal DrawCommandCollector DrawCmdCollector => _commandCollector;

    internal DrawCommandPipeline()
    {
    }

    public void Initialize(RenderSystemContext ctx, Action<IDrawCommandCollector> collectorSetup)
    {
        _ctx = ctx;

        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = ctx.Gfx, Registry = ctx.Registry, RenderView = ctx.View, Snapshot = ctx.Snapshot
        };
        var cmdProducerCtx = new CommandProducerContext { Gfx = ctx.Gfx, DrawBatchers = ctx.Batchers };

        //
        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _drawCmdProc = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        //
        _commandBuffer = new DrawCommandBuffer(_drawCmdProc, _drawBuffers);
        _materialBuffer = new MaterialDrawBuffer();

        //
        _commandCollector = new DrawCommandCollector();
        collectorSetup(_commandCollector);
        _sceneDrawProducer = _commandCollector.GetProducer<SceneDrawProducer>();
        _commandCollector.AttachContext(cmdProducerCtx);
        _commandCollector.InitializeProducers();

        //
        _commandBuffer.Initialize();
        _drawCmdProc.Initialize();
        _drawBuffers.AttachMaterialBuffer(_materialBuffer);
    }


    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    internal void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _materialBuffer.SubmitDrawData(in payload, slots);


    internal void Prepare(RenderSceneState snapshot)
    {
        _sceneDrawProducer.SetSceneGlobals(snapshot);

        _commandBuffer.Reset();
        _materialBuffer.Reset();

        _drawCmdProc.Prepare();
        _drawBuffers.ResetCursor();
    }

    internal void PrepareDrawBuffers()
    {
        // Fill command buffer
        _commandCollector.CollectTo(_ctx.CurrentFrameInfo.Alpha, _ctx.Snapshot, _commandBuffer);

        // Sort command buffer and prepare passes
        _commandBuffer.ReadyDrawCommands();

        // Fill Material buffer
        // Happens in engine atm

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_commandBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);

        _drawBuffers.EnsureDrawBuffers(drawCap, matCap);
    }

    internal void UploadUniformGlobals()
    {
        _drawBuffers.UploadGlobalUniforms(in _ctx.CurrentFrameInfo, in _ctx.CurrentRuntimeParams);
        _drawBuffers.UploadCameraView(_ctx.View);
    }

    internal void UploadDrawUniformData()
    {
        _drawBuffers.UploadMaterial(_materialBuffer.DrainDrawMaterialData());
        _drawBuffers.UploadDrawObjects(_commandBuffer.DrainTransformQueue());
    }

    internal void ExecuteDrawPass(PassId passId)
    {
        _drawBuffers.ResetCursor();
        _drawCmdProc.PrepareDrawPass();
        _commandBuffer.DispatchDrawPass(passId, _drawCmdProc);
    }
}