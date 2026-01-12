using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public sealed class TerrainMeshGenerator : MeshGenerator
{
    public AssetRef<Texture2D> TextureRef { get; private set; }
    public int MaxHeight { get; private set; }
    public int Step { get; private set; }
    public int Dimension { get; private set; }
    public int Size { get; private set; }
    public int VertexCount { get; private set; }
    public int DrawCount { get; private set; }

    private float[] _heights;
    private uint[] _indices;
    private Vertex3D[] _vertices;


    public MeshId MeshId { get; private set; }
    public VertexBufferId VboId { get; private set; }
    public IndexBufferId IboId { get; private set; }


    internal TerrainMeshGenerator(GfxContext gfx) : base(gfx)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(int x, int z)
    {
        x = Math.Clamp(x, 0, Dimension - 1);
        z = Math.Clamp(z, 0, Dimension - 1);
        return _heights[z * Dimension + x];
    }

    public void Initialize(Texture2D heightMap, int maxHeight, int step)
    {
        if (heightMap.PixelData is null)
            throw new ArgumentNullException(nameof(heightMap.PixelData));

        (int width, int height) = (heightMap.Width, heightMap.Height);
        var data = heightMap.PixelData!.Value.Span;
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 32);
        ArgumentOutOfRangeException.ThrowIfNotEqual(width, height);
        ArgumentOutOfRangeException.ThrowIfNotEqual(data.Length, width * width * 4);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(step, 16);

        TextureRef = new AssetRef<Texture2D>(heightMap.Id);
        Dimension = width;
        Size = width * width;
        MaxHeight = maxHeight;
        Step = step;
        BuildHeightMap(data, width, maxHeight);
    }

    public void BuildBatch()
    {
        int vertexRowCount = (Dimension - 1) / Step + 1;
        VertexCount = vertexRowCount * vertexRowCount;

        GenerateVertex(vertexRowCount);
        GenerateIndices(vertexRowCount);
        RecomputeNormalsFromIndices();
        GenerateMesh();

        MeshId.IsValidOrThrow();
    }


    public override void Dispose()
    {
        if (MeshId.IsValid())
            Gfx.Disposer.EnqueueRemoval(MeshId);
    }

    private void BuildHeightMap(ReadOnlySpan<byte> data, int width, int maxHeight)
    {
        var size = width * width;
        _heights = new float[size];

        for (int z = 0; z < width; z++)
        {
            int rowStart = z * width;
            for (int x = 0; x < width; x++)
            {
                _heights[rowStart + x] = SampleHeight(data, x, z, width, maxHeight);
            }
        }
    }


    private void GenerateMesh()
    {
        ArgumentNullException.ThrowIfNull(_vertices);
        ArgumentNullException.ThrowIfNull(_indices);
        ArgumentOutOfRangeException.ThrowIfLessThan(_vertices.Length, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(_indices.Length, 8);

        var drawCount = _indices.Length;

        var props = MeshDrawProperties.MakeElemental(drawCount: drawCount);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);
        builder.UploadVertices(_vertices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        builder.UploadIndices(_indices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var attribBuilder = new VertexAttributeMaker();
        builder.AddAttribute(attribBuilder.Make<Vector3>(0));
        builder.AddAttribute(attribBuilder.Make<Vector2>(1));
        builder.AddAttribute(attribBuilder.Make<Vector3>(2));
        builder.AddAttribute(attribBuilder.Make<Vector3>(3));
        MeshId = Gfx.Meshes.FinishUploadBuilder(out _);
    }

    private void GenerateVertex(int vertexRowCount)
    {
        _vertices = new Vertex3D[VertexCount];

        for (var vz = 0; vz < vertexRowCount; vz++)
        {
            var zPix = Math.Min(vz * Step, Dimension - 1);
            for (var vx = 0; vx < vertexRowCount; vx++)
            {
                var xPix = Math.Min(vx * Step, Dimension - 1);

                var y = GetHeight(xPix, zPix);
                var pos = new Vector3(xPix, y, zPix);
                var uv = new Vector2(xPix / (float)(Dimension - 1), zPix / (float)(Dimension - 1));

                var vi = vz * vertexRowCount + vx;
                _vertices[vi] = new Vertex3D(pos, uv, Vector3.UnitY, Vector3.UnitX);
            }
        }

        // Gram-Schmidt
        for (var vz = 0; vz < vertexRowCount; vz++)
        {
            for (var vx = 0; vx < vertexRowCount; vx++)
            {
                var vi = vz * vertexRowCount + vx;
                var n = GetNormal(vx, vz);
                var t = GetTangent(vx, vz, n);
                _vertices[vi].Normal = n;
                _vertices[vi].Tangent = t;
            }
        }
    }

    private void GenerateIndices(int vertexRowCount)
    {
        int quadCount = vertexRowCount - 1;

        _indices = new uint[quadCount * quadCount * 6];
        var indices = _indices.AsSpan();

        int k = 0;
        for (int z = 0; z < quadCount; z++)
        {
            for (int x = 0; x < quadCount; x++)
            {
                uint i0 = (uint)(z * vertexRowCount + x);
                uint i1 = i0 + 1;
                uint i2 = (uint)((z + 1) * vertexRowCount + x);
                uint i3 = i2 + 1;

                var h0 = _vertices[i0].Position.Y;
                var h1 = _vertices[i1].Position.Y;
                var h2 = _vertices[i2].Position.Y;
                var h3 = _vertices[i3].Position.Y;

                var diag = MathF.Abs(h0 - h3) <= MathF.Abs(h1 - h2);

                if (diag)
                {
                    indices[k++] = i0;
                    indices[k++] = i2;
                    indices[k++] = i3;
                    indices[k++] = i0;
                    indices[k++] = i3;
                    indices[k++] = i1;
                }
                else
                {
                    indices[k++] = i0;
                    indices[k++] = i2;
                    indices[k++] = i1;
                    indices[k++] = i1;
                    indices[k++] = i2;
                    indices[k++] = i3;
                }
            }
        }

        DrawCount = k;
    }

    private void RecomputeNormalsFromIndices()
    {
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i].Normal = Vector3.Zero;

        for (int i = 0; i < _indices.Length; i += 3)
        {
            uint i0 = _indices[i + 0], i1 = _indices[i + 1], i2 = _indices[i + 2];
            ref var v0 = ref _vertices[i0];
            ref var v1 = ref _vertices[i1];
            ref var v2 = ref _vertices[i2];

            var p0 = v0.Position;
            var p1 = v1.Position;
            var p2 = v2.Position;
            var e1 = p1 - p0;
            var e2 = p2 - p0;
            var fn = Vector3.Cross(e1, e2);

            v0.Normal += fn;
            v1.Normal += fn;
            v2.Normal += fn;
        }

        // normalize
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i].Normal = NormalizeSafe(_vertices[i].Normal);
    }

    private Vector3 GetTangent(int vx, int vz, Vector3 n)
    {
        var xPix = Math.Min(vx * Step, Dimension - 1);
        var zPix = Math.Min(vz * Step, Dimension - 1);
        var hL = GetHeight(xPix - Step, zPix);
        var hR = GetHeight(xPix + Step, zPix);

        var rawT = new Vector3(2 * Step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }


    private Vector3 GetNormal(int vx, int vz)
    {
        var xPix = Math.Min(vx * Step, Dimension - 1);
        var zPix = Math.Min(vz * Step, Dimension - 1);

        var hL = GetHeight(xPix - Step, zPix);
        var hR = GetHeight(xPix + Step, zPix);
        var hD = GetHeight(xPix, zPix - Step);
        var hU = GetHeight(xPix, zPix + Step);

        var dx = new Vector3(2 * Step, hR - hL, 0f);
        var dz = new Vector3(0f, hU - hD, 2 * Step);

        return NormalizeSafe(Vector3.Cross(dz, dx));
    }

    private static float SampleHeight(ReadOnlySpan<byte> data, int x, int z, int dimension, int maxHeight)
    {
        x = Math.Clamp(x, 0, dimension - 1);
        z = Math.Clamp(z, 0, dimension - 1);

        const int channels = 4;

        var rowStrideBytes = data.Length / dimension;

        var idx = z * rowStrideBytes + x * channels;
        if ((uint)(idx + channels - 1) >= (uint)data.Length)
            return 0f;

        byte r = data[idx];
        return r / 255f * maxHeight;
    }

    private static Vector3 NormalizeSafe(Vector3 v)
    {
        var len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : Vector3.UnitY;
    }
}