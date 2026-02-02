using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
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
    Span<VertexSkinned> vertices,
    Span<SkinningData> skinned,
    Span<uint> indices)
{
    public readonly Span<VertexSkinned> Vertices = vertices;
    public readonly Span<SkinningData> Skinned = skinned;
    public readonly Span<uint> Indices = indices;
}

public sealed class MeshScratchpad
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

    internal static void Initialize(int defaultVertexCapacity = DefaultVertexCap)
    {
        if (Instance is not null)
            throw new InvalidOperationException(nameof(Instance));

        Instance = new MeshScratchpad(defaultVertexCapacity);
    }
    //

    private uint[] _indices;
    private Vertex3D[] _vertices;
    private VertexSkinned[] _verticesSkinned;
    private SkinningData[] _skinned;

    private readonly List<MeshRange> _meshRanges = new(8);
    private bool _active;

    private MeshScratchpad(int defaultCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultCapacity, 64);
        _vertices = new Vertex3D[defaultCapacity];
        _verticesSkinned = new VertexSkinned[defaultCapacity];
        _skinned = new SkinningData[defaultCapacity];
        _indices = new uint[defaultCapacity * 3];
    }

    public bool IsBound => _active;
    public int MeshCount => _meshRanges.Count;

    public int VertexCapacity => _vertices.Length;
    public int IndexCapacity => _indices.Length;

    public long BufferSize =>
        (long)_vertices.Length * Unsafe.SizeOf<Vertex3D>() +
        (long)_verticesSkinned.Length * Unsafe.SizeOf<VertexSkinned>() +
        (long)_indices.Length * sizeof(uint);

    public void Begin(Span<(int vertexCount, int indexCount)> dataCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dataCount.Length);

        if (_active) throw new InvalidOperationException(nameof(IsBound));

        _meshRanges.Clear();
        _active = true;

        int vertexCursor = 0, indexCursor = 0;
        foreach (var (vertexCount, indexCount) in dataCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(indexCount);

            _meshRanges.Add(new MeshRange((vertexCursor, vertexCount), (indexCursor, indexCount)));

            vertexCursor += vertexCount;
            indexCursor += indexCount;
        }

        EnsureCapacity(vertexCursor, indexCursor);
    }

    public void End()
    {
        _meshRanges.Clear();
        _active = false;
    }

    //
    public MeshDataSpan GetMeshSpan(int meshIndex)
    {
        if (!_active) throw new InvalidOperationException(nameof(IsBound));

        if ((uint)meshIndex >= _meshRanges.Count)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));

        var range = _meshRanges[meshIndex];

        return new MeshDataSpan(
            _vertices.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _indices.AsSpan(range.Index.Offset, range.Index.Length));
    }

    public MeshSkinnedDataSpan GetSkinnedMeshSpan(int meshIndex)
    {
        if (!_active) throw new InvalidOperationException(nameof(IsBound));

        if ((uint)meshIndex >= _meshRanges.Count)
            throw new ArgumentOutOfRangeException(nameof(meshIndex));

        var range = _meshRanges[meshIndex];

        return new MeshSkinnedDataSpan(
            _verticesSkinned.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _skinned.AsSpan(range.Vertex.Offset, range.Vertex.Length),
            _indices.AsSpan(range.Index.Offset, range.Index.Length));
    }

    public void EnsureCapacity(int vertexCount, int indexCount)
    {
        if (vertexCount > _vertices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(_vertices.Length);
            Array.Resize(ref _vertices, cap);
            Array.Resize(ref _verticesSkinned, cap);
            Array.Resize(ref _skinned, cap);
            Console.WriteLine($"VertexArray: Large buffer resize {_vertices.Length} to {cap}", LogLevel.Warn);
        }

        if (indexCount > _indices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(indexCount);
            Array.Resize(ref _indices, cap);
            Console.WriteLine($"IndexArray: Large buffer resize {_indices.Length} to {cap}", LogLevel.Warn);
        }
    }
}