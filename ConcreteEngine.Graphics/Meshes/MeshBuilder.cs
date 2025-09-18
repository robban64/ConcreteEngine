using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public sealed class MeshDescriptorBuilder
{
    private readonly CommonBuilder<Builder, Result, State> _builder = new(new Builder(), () => new State());

    public MeshDescriptorBuilder()
    {
    }

    public Builder CreateBuilder()
    {
        return _builder.CreateBuilder();
    }

    public Result BuildMesh()
    {
        return _builder.Build();
    }

    public sealed class Builder : CommonBuilderBase<Result, State>
    {
        public Builder AddVertices<V>(ReadOnlySpan<V> vertices, BufferUsage usage, BufferStorage storage,
            BufferAccess access) where V : unmanaged
        {
            var idx = State.VertexBufferDesc.Count;

            if (State.VertexData.Count < idx)
                State.VertexData.Add(new MemoryDataBuffer(MemoryDataBuffer.DefaultCapacityVbo));

            State.VertexData[idx].SetData(vertices);

            State.VertexBufferDesc.Add(new VertexBufferDesc
            {
                BindingIdx = (uint)idx,
                VertexCount = (uint)vertices.Length,
                VertexSize = (uint)Unsafe.SizeOf<V>(),
                Usage = usage,
                Storage = storage,
                Access = access
            });
            return this;
        }

        public Builder WithIndices<I>(ReadOnlySpan<I> indices, BufferUsage usage, BufferStorage storage,
            BufferAccess access) where I : unmanaged
        {
            InvalidOpThrower.ThrowIf(State.IndexBufferDesc.HasValue);
            State.IndexData ??= new MemoryDataBuffer(MemoryDataBuffer.DefaultCapacityIbo);
            State.IndexData.SetData(indices);
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


        protected override void ValidateBuilder(State s)
        {
            InvalidOpThrower.ThrowIfNull(s);
            InvalidOpThrower.ThrowIf(s.VertexBufferDesc.Count == 0, nameof(s.VertexBufferDesc));
            InvalidOpThrower.ThrowIf(s.VertexData.Count != s.VertexBufferDesc.Count, nameof(s.VertexData));
        }

        protected override Result BuildResult(State state)
        {
        }

        protected override void ResetBuilder(State state)
        {
            foreach (var data in state.VertexData)
                data.ResetCursor();

            state.Attributes.Clear();
            state.VertexBufferDesc.Clear();
            state.IndexBufferDesc = null;
            state.MeshProperties = null;
        }

        public void ClearData()
        {
            foreach (var vboData in State.VertexData)
                vboData.ClearData();
        }
    }


    public sealed class State : IBuilderState
    {
        public List<VertexAttributeDescriptor> Attributes { get; } = new();
        public MeshDrawProperties? MeshProperties { get; set; } = null;
        public List<VertexBufferDesc> VertexBufferDesc { get; } = new();
        public IndexBufferDesc? IndexBufferDesc { get; set; } = null;
        public List<MemoryDataBuffer> VertexData { get; } = new();
        public MemoryDataBuffer? IndexData { get; set; }
    }

    public sealed class Result
    {
        public required VertexAttributeDescriptor[] Attributes { get; init; }
        public required VertexBufferPayload[] VertexBuffers { get; init; }
        public required IndexBufferPayload? IndexBuffer { get; init; }
        public required uint DrawCount { get; init; }
    }
}