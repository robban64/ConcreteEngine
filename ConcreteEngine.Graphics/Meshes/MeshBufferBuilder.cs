using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;

namespace ConcreteEngine.Graphics;

public sealed class MeshBufferBuilder
{
    public const int DefaultCapacityVbo = 1024 * 12;
    public const int DefaultCapacityIbo = 1024 * 4;

    private readonly CommonBuilder<Builder, Result, State> _builder;

    private readonly List<MemoryDataBuffer> _vboBuffers;
    private readonly MemoryDataBuffer _iboBuffer;

    public MeshBufferBuilder()
    {
        _vboBuffers = [new MemoryDataBuffer(DefaultCapacityVbo), new MemoryDataBuffer(DefaultCapacityVbo)];
        _iboBuffer = new MemoryDataBuffer(DefaultCapacityIbo);
        var builderObj = new Builder(_vboBuffers, _iboBuffer);
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

    public void ClearBufferData()
    {
        _iboBuffer.ClearData();
        foreach (var vboData in _vboBuffers)
            vboData.ClearData();
    }

    public sealed class Builder : CommonBuilderBase<Result, State>
    {
        private readonly List<VertexBufferPayload> _vboResultListCache = new(4);
        private readonly Result _result = new();

        private readonly List<MemoryDataBuffer> _vboDataBuffers;
        private readonly MemoryDataBuffer _iboDataBuffer;

        internal Builder(List<MemoryDataBuffer> vboDataBuffers, MemoryDataBuffer iboDataBuffer)
        {
            _vboDataBuffers = vboDataBuffers;
            _iboDataBuffer = iboDataBuffer;
        }

        public Builder AddVertices<V>(ReadOnlySpan<V> vertices, BufferUsage usage, BufferStorage storage,
            BufferAccess access) where V : unmanaged
        {
            var idx = State.VertexBufferDesc.Count;

            if (_vboDataBuffers.Count < idx)
                _vboDataBuffers.Add(new MemoryDataBuffer(DefaultCapacityVbo));

            _vboDataBuffers[idx].SetData(vertices);

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
            var drawElementSize = GfxUtilsEnum.ToDrawElementSize<I>();
            _iboDataBuffer.SetData(indices);
            State.IndexBufferDesc = new IndexBufferDesc
            {
                ElementCount = (uint)indices.Length,
                ElementSize = (uint)Unsafe.SizeOf<I>(),
                Usage = usage,
                Storage = storage,
                Access = access
            };
            State.DrawProperties = State.DrawProperties with { ElementSize = drawElementSize };
            return this;
        }

        public Builder AddAttribute(in VertexAttributeDesc attr)
        {
            State.Attributes.Add(attr);
            return this;
        }

        protected override void StartBuilder(State state)
        {
        }

        protected override void ValidateBuilder(State s)
        {
            InvalidOpThrower.ThrowIfNull(s);
            InvalidOpThrower.ThrowIfNullOrEmpty(s.Attributes);
            InvalidOpThrower.ThrowIfNullOrEmpty(s.VertexBufferDesc);

            InvalidOpThrower.ThrowIf(s.VertexBufferDesc.Count == 0, nameof(s.VertexBufferDesc));

            if (s.DrawProperties.DrawKind == MeshDrawKind.Elements)
                if (State.IndexBufferDesc is null || State.DrawProperties.ElementSize == DrawElementSize.Invalid)
                    throw new InvalidOperationException(nameof(s.DrawProperties.DrawKind));
        }

        protected override Result BuildResult(State state)
        {
            if (_vboResultListCache.Count > 0)
                throw new InvalidOperationException(nameof(_vboResultListCache));

            IMeshPayload payload = null!;

            for (var i = 0; i < state.VertexBufferDesc.Count; i++)
            {
                var buffer = _vboDataBuffers[i];
                var descriptor = state.VertexBufferDesc[i];
                _vboResultListCache.Add(new VertexBufferPayload(in descriptor, buffer.AsReadOnlyMemory()));
            }

            IndexBufferPayload? iboPayloadN = null;
            if (state.IndexBufferDesc is { } indexBufferDesc)
            {
                var data = _iboDataBuffer.AsReadOnlyMemory();
                iboPayloadN = new IndexBufferPayload(in indexBufferDesc, data);
            }

            if (iboPayloadN is { } iboPayload)
            {
                 _result.MeshPayload = new MeshPayloadIndexed
                {
                    DrawProperties = State.DrawProperties,
                    Attributes = State.Attributes.ToArray(),
                    VertexBuffers = _vboResultListCache,
                    IndexBuffer = iboPayload
                };
                 return _result;
            }

            _result.MeshPayload = new MeshPayloadBasic
            {
                DrawProperties = State.DrawProperties,
                Attributes = State.Attributes,
                VertexBuffers = _vboResultListCache,
            };
            return _result;
        }

        protected override void ResetBuilder(State state)
        {
            _vboResultListCache.Clear();

            foreach (var buffer in _vboDataBuffers)
                buffer.ResetCursor();

            _iboDataBuffer.ResetCursor();

            state.Attributes.Clear();
            state.VertexBufferDesc.Clear();
            state.IndexBufferDesc = null;
            state.DrawProperties = MeshDrawProperties.MakeDefault();
        }
    }


    public sealed class State : IBuilderState
    {
        public List<VertexAttributeDesc> Attributes { get; } = new(4);

        public MeshDrawProperties DrawProperties { get; set; } = MeshDrawProperties.MakeDefault();

        public List<VertexBufferDesc> VertexBufferDesc { get; } = new(4);
        public IndexBufferDesc? IndexBufferDesc { get; set; } = null;

    }

    public sealed class Result
    {
        public IMeshPayload MeshPayload { get; set; } = null!;
    }
}