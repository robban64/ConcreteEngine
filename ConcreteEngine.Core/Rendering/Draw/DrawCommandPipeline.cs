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

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector;
    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _cmdBuffer;
    private MaterialDrawBuffer _materialBuffer;

    private DrawCommandProcessor _cmdDraw;
    private DrawBuffers _drawBuffers;
    private DrawStateOps _drawStateOps;

    internal DrawStateOps DrawStateOps => _drawStateOps;
    internal DrawBuffers DrawBuffer => _drawBuffers;
    internal DrawCommandProcessor DrawCmdProcessor => _cmdDraw;

    internal DrawCommandCollector DrawCmdCollector => _commandCollector;

    internal DrawCommandPipeline()
    {
    }

    public void Initialize(RenderSystemContext ctx, Action<IDrawCommandCollector> collectorSetup)
    {
        var drawCtx = new DrawStateContext(ctx.Registry);
        var drawCtxPayload = new DrawStateContextPayload
        {
            Gfx = ctx.Gfx, Registry = ctx.Registry, RenderView = ctx.View, Snapshot = ctx.Snapshot
        };
        var cmdProducerCtx = new CommandProducerContext { Gfx = ctx.Gfx, DrawBatchers = ctx.Batchers };

        _drawBuffers = new DrawBuffers(drawCtx, drawCtxPayload);
        _cmdDraw = new DrawCommandProcessor(drawCtx, drawCtxPayload, _drawBuffers);
        _drawStateOps = new DrawStateOps(drawCtx, drawCtxPayload, _drawBuffers);

        _cmdBuffer = new DrawCommandBuffer(_cmdDraw, _drawBuffers);
        _materialBuffer = new MaterialDrawBuffer();

        _commandCollector = new DrawCommandCollector();
        //

        collectorSetup(_commandCollector);
        _sceneDrawProducer = _commandCollector.GetProducer<SceneDrawProducer>();
        _commandCollector.AttachContext(cmdProducerCtx);
        _commandCollector.InitializeProducers();


        //
        _cmdBuffer.Initialize();
        _cmdDraw.Initialize();
        _drawBuffers.AttachMaterialBuffer(_materialBuffer);
    }


    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();

    internal void SubmitMaterialDrawData(in DrawMaterialPayload payload, ReadOnlySpan<TextureSlotInfo> slots) =>
        _materialBuffer.SubmitDrawData(in payload, slots);

    internal (nint, nint) Prepare(float alpha, RenderSceneState snapshot)
    {
        _cmdBuffer.Reset();
        _materialBuffer.Reset();

        _sceneDrawProducer.SetSceneGlobals(snapshot);

        // Fill command buffer
        _commandCollector.CollectTo(alpha, snapshot, _cmdBuffer);

        // Sort command buffer and prepare passes
        _cmdBuffer.ReadyDrawCommands();

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_cmdBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);
        return (drawCap, matCap);
    }

    internal void ExecuteMaterials()
    {
        _drawBuffers.UploadMaterial(_materialBuffer.DrainDrawMaterialData());
    }

    internal void ExecuteTransforms() => _cmdBuffer.DrainTransformQueue();

    internal void ExecuteDrawPass(PassId passId) => _cmdBuffer.DispatchDrawPass(passId);
}