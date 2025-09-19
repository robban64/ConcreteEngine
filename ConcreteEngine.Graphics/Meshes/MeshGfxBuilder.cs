using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Graphics;


public sealed class MeshGfxBuilder
{
    private readonly CommonBuilder<Builder, Result, State> _builder;

    internal MeshGfxBuilder(GfxResourceAllocator allocator)
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
            var drawElementSize = GfxEnumUtils.ToDrawElementSize<I>();
            
            State.IboId = Gfx.CreateIndexBuffer(indices, usage);
            State.DrawProperties = State.DrawProperties with { ElementSize = drawElementSize };
            return this;
        }

        public Builder AddAttribute(in VertexAttributeDesc attr)
        {
            State.Attributes.Add(attr);
            return this;
        }


        protected override void ValidateBuilder(State s)
        {
            InvalidOpThrower.ThrowIfNot(s.MeshId.IsValid(), nameof(s.MeshId));
            InvalidOpThrower.ThrowIfNullOrEmpty(s.VboIds, nameof(s.VboIds));
            InvalidOpThrower.ThrowIfNullOrEmpty(s.Attributes, nameof(s.Attributes));
            InvalidOpThrower.ThrowIfNullOrEmpty(s.Attributes, nameof(s.Attributes));
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
}