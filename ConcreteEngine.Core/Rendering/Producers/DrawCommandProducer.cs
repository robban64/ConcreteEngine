#region

using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public sealed class CommandProducerContext
{
    public IWorld World { get; set; } = null!;
    public required GfxContext Gfx { get; init; }
    public required BatcherRegistry DrawBatchers { get; init; }
}

public interface IDrawCommandProducer
{
    void AttachContext(CommandProducerContext ctx);
    void Initialize();
    void BeginTick(in UpdateTickInfo tick);
    void EndTick();
    void EmitFrame(float alpha, in RenderSceneState snapshot, DrawCommandBuffer submitter);
}

public interface IDrawSink;