using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Graphics;

public readonly ref struct MeshDataSpan(Span<Vertex3D> vertices, Span<uint> indices)
{
    public readonly Span<Vertex3D> Vertices = vertices;
    public readonly Span<uint> Indices = indices;
}

public readonly ref struct MeshSkinnedDataSpan(
    Span<Vertex3D> vertices,
    Span<SkinningData> skinned,
    Span<uint> indices)
{
    public readonly Span<Vertex3D> Vertices = vertices;
    public readonly Span<SkinningData> Skinned = skinned;
    public readonly Span<uint> Indices = indices;
}

public sealed class MeshScratchpad : IDisposable
{
    private const int DefaultVertexCap = 128_000;

    //
    private readonly struct MeshRange(Range32 vertex, Range32 index)
    {
        public readonly Range32 Vertex = vertex;
        public readonly Range32 Index = index;
    }

    //
    internal static MeshScratchpad Instance { get; private set; } = null!;

    internal static void Initialize()
    {
        if (Instance is not null)
            throw new InvalidOperationException(nameof(Instance));

        Instance = new MeshScratchpad();
    }
    //

    private static NativeArray<uint> _indices;
    private static NativeArray<Vertex3D> _vertices;
    private static NativeArray<SkinningData> _skinned;

    private readonly MeshRange[] _meshRanges = new MeshRange[16];

    public int MeshCount { get; private set; }
    public bool IsBound { get; private set; }

    private MeshScratchpad()
    {
        if (!_indices.IsNull || !_vertices.IsNull || !_skinned.IsNull)
            throw new InvalidOperationException("Vertex Arrays already allocated");

        _indices = NativeArray.Allocate<uint>(DefaultVertexCap * 3);
        _vertices = NativeArray.Allocate<Vertex3D>(DefaultVertexCap);
        _skinned = NativeArray.Allocate<SkinningData>(DefaultVertexCap);
    }


    public int VertexLength => _vertices.Length;
    public int IndexLength => _indices.Length;

    public long BufferSize =>
        (long)_vertices.SizeInBytes + (long)_skinned.SizeInBytes + (long)_indices.SizeInBytes;

    public void Begin(Span<(int vertexCount, int indexCount)> dataCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dataCount.Length);

        if (IsBound) throw new InvalidOperationException(nameof(IsBound));

        MeshCount = dataCount.Length;
        IsBound = true;

        int vertexCursor = 0, indexCursor = 0;
        for (var i = 0; i < dataCount.Length; i++)
        {
            var (vertexCount, indexCount) = dataCount[i];
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

            _meshRanges[i] = new MeshRange((vertexCursor, vertexCount), (indexCursor, indexCount));

            vertexCursor += vertexCount;
            indexCursor += indexCount;
        }

        EnsureCapacity(vertexCursor, indexCursor);
    }

    public void End()
    {
        Array.Clear(_meshRanges);
        MeshCount = 0;
        IsBound = false;
    }

    //
    public MeshDataSpan GetMeshSpan(int meshIndex)
    {
        if (!IsBound) throw new InvalidOperationException(nameof(IsBound));

        if ((uint)meshIndex >= MeshCount)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));

        var range = _meshRanges[meshIndex];

        return new MeshDataSpan(
            _vertices.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _indices.AsSpan(range.Index.Offset, range.Index.Length));
    }

    public MeshSkinnedDataSpan GetSkinnedMeshSpan(int meshIndex)
    {
        if (!IsBound) throw new InvalidOperationException(nameof(IsBound));

        if ((uint)meshIndex >= MeshCount)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));

        var range = _meshRanges[meshIndex];

        return new MeshSkinnedDataSpan(
            _vertices.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _skinned.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _indices.AsSpan(range.Index.Offset, range.Index.Length));
    }

    public void EnsureCapacity(int vertexCount, int indexCount)
    {
        if (vertexCount > _vertices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(_vertices.Length);
            _vertices.Resize(cap, false);
            _skinned.Resize(cap, false);
            Console.WriteLine($"VertexArray: Large buffer resize {_vertices.Length} to {cap}", LogLevel.Warn);
        }

        if (indexCount > _indices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(indexCount);
            _indices.Resize(cap, false);
            Console.WriteLine($"IndexArray: Large buffer resize {_indices.Length} to {cap}", LogLevel.Warn);
        }
    }

    public void Dispose()
    {
        _indices.Dispose();
        _vertices.Dispose();
        _skinned.Dispose();
    }
}