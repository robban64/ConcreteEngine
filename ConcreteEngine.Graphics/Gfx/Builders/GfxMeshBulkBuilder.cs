using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx.Builders;

/*
public sealed class GfxMeshBulkBuilder
{
    private readonly CommonBuilder<Builder, Result, State> _builder;

    internal GfxMeshBulkBuilder(GfxResourceAllocator allocator)
    {
        var builderObj = new Builder(allocator);
        _builder = new CommonBuilder<Builder, Result, State>(builderObj, static () => new State());
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
        private readonly Result _result = new();
        
        internal GfxResourceAllocator Gfx { get; init; }


        internal Builder(GfxResourceAllocator gfx)
        {
            Gfx = gfx;
        }
        
        
        protected override void StartBuilder(State state)
        {
            State.MeshId = Gfx.CreateEmptyMesh();
        }

        public Builder SetDrawProperties<V>(in MeshDrawProperties prop) where V : unmanaged
        {
            State.DrawProperties  = prop;
            return this;
        }

        public Builder AddVertices<V>(ReadOnlySpan<V> vertices, BufferUsage usage, BufferStorage storage,
            BufferAccess access) where V : unmanaged
        {
            var vboId = Gfx.CreateVertexBuffer(vertices, usage, (uint)State.VboIds.Count);
            State.VboIds.Add(vboId);
            return this;
        }

        public Builder WithIndices<I>(ReadOnlySpan<I> indices, BufferUsage usage, BufferStorage storage,
            BufferAccess access) where I : unmanaged
        {
            InvalidOpThrower.ThrowIf(State.IboId.IsValid());
            var drawElementSize = GfxUtilsEnum.ToDrawElementSize<I>();
            
            State.IboId = Gfx.CreateIndexBuffer(indices, usage);
            State.DrawProperties = State.DrawProperties with { ElementSize = drawElementSize };
            return this;
        }

        public Builder WithAttributes(IReadOnlyList<VertexAttributeDesc> attr)
        {
            Gfx.SetVertexAttribute(State.MeshId, attr);
            State.Attributes.AddRange(attr);
            return this;
        }


        protected override void ValidateBuilder(State s)
        {
            InvalidOpThrower.ThrowIfNot(s.MeshId.IsValid(), nameof(s.MeshId));
            InvalidOpThrower.ThrowIfNullOrEmptyCollection(s.VboIds, nameof(s.VboIds));
            InvalidOpThrower.ThrowIfNullOrEmptyCollection(s.Attributes, nameof(s.Attributes));
            InvalidOpThrower.ThrowIfNullOrEmptyCollection(s.Attributes, nameof(s.Attributes));
            foreach (var vboId in s.VboIds)
                InvalidOpThrower.ThrowIfNot(vboId.IsValid(), nameof(vboId));

        }

        protected override Result BuildResult(State state)
        {
            return _result;
        }

        protected override void ResetBuilder(State state)
        {
            State.MeshId = default;
            State.IboId = default;
            State.DrawProperties = MeshDrawProperties.MakeDefault();
            State.VboIds.Clear();
            State.Attributes.Clear();
        }
    }


    public sealed class State : IBuilderState
    {
        public MeshId MeshId { get; set; }
        public IndexBufferId IboId { get; set; }
        public List<VertexBufferId> VboIds { get; set; } = new();
        public List<VertexAttributeDesc> Attributes { get; set; } = new();
        public MeshDrawProperties DrawProperties { get; set; } = MeshDrawProperties.MakeDefault();


    }

    public sealed class Result
    {
        public IMeshPayload MeshPayload { get; set; } = null!;
    }
}*/