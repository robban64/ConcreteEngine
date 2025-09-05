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
    public required IGraphicsDevice Graphics { get; init; }
    public required BatcherRegistry DrawBatchers { get; init; }
}

public interface IDrawCommandProducer
{
    void AttachContext(CommandProducerContext ctx);
    void Initialize();
    void BeginTick(in UpdateMetaInfo updateMeta);
    void EndTick();
    void EmitFrame(float alpha, IRenderPipeline submitter);
}

public interface IDrawSink;


internal sealed class DrawProduceArray<T> where T : unmanaged
{
    public T[] Data;
    public Span<T> AsSpan() => Data.AsSpan();
    public Span<T> AsSpan(int start) => Data.AsSpan(start);
    public Span<T> AsSpan(int start, int length) => Data.AsSpan(start, length);


    public DrawProduceArray(int capacity)
    {
        Data = new T[capacity];
    }

    public void EnsureCapacity(int size)
    {
        if (Data.Length < size)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 50_000);
            var newSize = int.Max(Data.Length * 2, size);
            Array.Resize(ref Data, newSize);
        }
    }

}