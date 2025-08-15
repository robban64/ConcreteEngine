#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Resources;

public interface IVertexBuffer : IGraphicsBuffer;

public interface IIndexBuffer : IGraphicsBuffer;

public interface IGraphicsBuffer : IGraphicsResource
{
    BufferUsage Usage { get; }
    BufferTarget Target { get; }
    bool IsStatic { get; }
    int ElementCount { get; }
    int ElementSize { get; }
    int BufferSizeInBytes { get; }
}