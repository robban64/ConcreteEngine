#region

using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Core.Rendering.Producers;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

internal sealed class DrawCommandPipeline
{
    private DrawCommandCollector _commandCollector = null!;

    private SceneDrawProducer _sceneDrawProducer = null!;

    private DrawCommandBuffer _cmdBuffer = null!;
    private DrawMaterialBuffer _materialBuffer;

    internal DrawCommandPipeline()
    {
    }

    public void Initialize(GfxContext gfx, BatcherRegistry batches, DrawProcessor drawProcessor)
    {
        _commandCollector = new DrawCommandCollector();
        _cmdBuffer = new DrawCommandBuffer(drawProcessor);
        _materialBuffer = new DrawMaterialBuffer(drawProcessor);

        _commandCollector.RegisterProducerSink<IMeshDrawSink>(new MeshDrawProducer());
        _commandCollector.RegisterProducerSink<ITerrainDrawSink>(new TerrainDrawProducer());
        _sceneDrawProducer = new SceneDrawProducer();
        _commandCollector.RegisterProducer<SceneDrawProducer>(_sceneDrawProducer);

        var cmdProducerCtx = new CommandProducerContext { Gfx = gfx, DrawBatchers = batches };
        _commandCollector.AttachContext(cmdProducerCtx);
        _cmdBuffer.Initialize();
        _commandCollector.InitializeProducers();
    }

    internal void BeginTick(in UpdateTickInfo tick) => _commandCollector.BeginTick(tick);
    internal void EndTick() => _commandCollector.EndTick();
    public TSink GetSink<TSink>() where TSink : IDrawSink => _commandCollector.GetSink<TSink>();


    // TODO fix (dont put store here)
    public void PrepareMaterials(MaterialStore materials)
    {
        var count = materials.MaterialSpan.Length;
        var span = materials.MaterialSpan;

        Span<DrawMaterialPayload> buffer = stackalloc DrawMaterialPayload[count];

        for (var i = 0; i < count; i++)
        {
            var material = span[i];
            if (material is null)
            {
                buffer[i] = default;
                continue;
            }

            materials.GetMaterialUploadData(material!, out var data);
            buffer[i] = data;
        }

        _materialBuffer.SubmitMaterials(buffer);
    }

    internal (nint, nint) Prepare(float alpha, RenderSceneState snapshot, MaterialStore materials)
    {
        _cmdBuffer.Reset();
        _materialBuffer.Reset();

        _sceneDrawProducer.SetSceneGlobals( snapshot);

        // Fill command buffer
        _commandCollector.CollectTo(alpha,  snapshot, _cmdBuffer);

        // Sort command buffer and prepare passes
        _cmdBuffer.ReadyDrawCommands();

        // Fill materials
        PrepareMaterials(materials);

        var drawCap = UniformBufferUtils.GetCapacityForEntities<DrawObjectUniform>(_cmdBuffer.Count + 32);
        var matCap = UniformBufferUtils.GetCapacityForEntities<MaterialUniformRecord>(_materialBuffer.Count + 4);
        return (drawCap, matCap);
    }

    internal void ExecuteMaterials() => _materialBuffer.DispatchMaterials();

    internal void ExecuteTransforms() => _cmdBuffer.DrainTransformQueue();

    internal void ExecuteDrawPass(PassId passId) => _cmdBuffer.DispatchDrawPass(passId);
}