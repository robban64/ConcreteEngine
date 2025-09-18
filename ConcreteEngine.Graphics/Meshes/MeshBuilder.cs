using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public readonly record struct VertexBufferDesc(
    VertexBufferId VboId,
    uint BindingIdx,
    uint VertexSize,
    uint VertexCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access);
    
public readonly record struct IndexBufferDesc(
    IndexBufferId IboId,
    uint ElementSize,
    uint ElementCount,
    BufferUsage Usage,
    BufferStorage Storage,
    BufferAccess Access);

public readonly record struct MeshDrawProperties(
    DrawPrimitive Primitive,
    MeshDrawKind DrawKind,
    DrawElementSize ElementSize,
    uint DrawCount
);

public sealed class State : IBuilderState
{
    public List<VertexAttributeDescriptor> Attributes { get; } = new();
    public MeshDrawProperties?  MeshProperties { get; set; } = null;
    public List<VertexBufferDesc> VertexBufferDesc { get; } = new();
    public IndexBufferDesc? IndexBufferDesc { get; set; } = null;
    public List<MemoryDataBuffer> VertexData { get; } = new();
    public MemoryDataBuffer IndexData { get; } = new();
}

public sealed class Result
{
    public VertexAttributeDescriptor[] Attributes { get; }
    public VertexBufferDesc[] VertexBuffers { get; }
    public IndexBufferDesc? IndexBuffer { get; }
    uint DrawCount { get; }
}


public sealed class MeshDescriptorBuilder
{
    private readonly CommonBuilder<Builder, Result, State> _builder = new(new Builder(), () => new State());

    public MeshDescriptorBuilder()
    {
        
    }

    public Builder CreateBuilder()
    {
        _builder.CreateBuilder();
    }
    
    public Builder BuildMesh()
    {
        return _builder.CreateBuilder();
    }
    
    
    public sealed class Builder : CommonBuilderBase<Result, State>
    {
        public Builder AddVertices<V>(ReadOnlySpan<V> vertices, BufferUsage usage, BufferStorage storage, BufferAccess access) where V : unmanaged
        {
            State.VertexBufferDesc.Add(new VertexBufferDesc
            {
                BindingIdx = (uint)State.VertexBufferDesc.Count,
                VertexCount = (uint)vertices.Length,
                VertexSize = (uint)Unsafe.SizeOf<V>(),
                Usage = usage,
                Storage = storage,
                Access = access
            });
            return this;
        }

        public Builder WithIndices<I>(ReadOnlySpan<I> indices, BufferUsage usage, BufferStorage storage, BufferAccess access) where I : unmanaged
        {
            StructThrower.ThrowIfNotNullStruct(State.IndexBufferDesc);
        
            State.IndexBufferDesc = new IndexBufferDesc
            {
                ElementCount = (uint)indices.Length,
                ElementSize = (uint)Unsafe.SizeOf<I>(),
                Usage = usage,
                Storage = storage,
                Access = access
            };
            return this;
        }

        public Builder AddAttribute(in VertexAttributeDescriptor attr)
        {
            State.Attributes.Add(attr);
            return this;
        }
        
        protected override void ResetBuilder(State state)
        {
            state.Attributes.Clear();
            state.VertexBufferDesc.Clear();
            state.IndexBuffer = null;
            state.MeshProperties = null;
        }

        protected override Result BuildResult(State state)
        {
            throw new NotImplementedException();
        }
    }

}