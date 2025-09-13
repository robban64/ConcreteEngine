#region

using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class CommandProducerContext
{
    public IWorld World { get; set; } = null!;
    public required IGraphicsRuntime Graphics { get; init; }
    public required BatcherRegistry DrawBatchers { get; init; }
}

public interface IDrawCommandProducer
{
    void AttachContext(CommandProducerContext ctx);
    void Initialize();
    void BeginTick(in UpdateInfo update);
    void EndTick();
    void EmitFrame(float alpha, in RenderGlobalSnapshot snapshot, RenderPipeline submitter);
}

public interface IDrawSink;

