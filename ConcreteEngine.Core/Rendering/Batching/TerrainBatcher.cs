using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Rendering;

public readonly struct TerrainBatchResult(MeshId meshId, int drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly int DrawCount = drawCount;
}

public sealed class TerrainBatcher : RenderBatcher<TerrainBatchResult>
{
    public Texture2D HeightMap { get; private set; }
    public int MaxHeight { get; private set; }
    public int Step { get; private set; }

    public int Size { get; private set; }
    public int VertexCount { get; private set; }
    public MeshId MeshId { get; private set; }
    public int DrawCount { get; private set; }

    private Vertex3D[] _vertices;
    private uint[] _indices;

    internal TerrainBatcher(GfxContext gfx) : base(gfx)
    {
    }

    public void Initialize(Texture2D heightMap, int maxHeight, int step)
    {
        HeightMap = heightMap;
        Size = HeightMap.Width;
        MaxHeight = maxHeight;
        Step = step;
    }

    public override TerrainBatchResult BuildBatch()
    {
        ArgumentNullException.ThrowIfNull(HeightMap.PixelData);
        ArgumentOutOfRangeException.ThrowIfLessThan(HeightMap.Width, 32);
        ArgumentOutOfRangeException.ThrowIfNotEqual(HeightMap.Width, HeightMap.Height);
        ArgumentOutOfRangeException.ThrowIfNotEqual(HeightMap.PixelData.Value.Length, Size * Size * 4);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Step, 16);

        var data = HeightMap.PixelData.Value;


        int vertexRowCount = ((Size - 1) / Step) + 1;
        VertexCount = vertexRowCount * vertexRowCount;

        var heightmap = data.Span;

        GenerateVertex(heightmap, vertexRowCount);
        GenerateIndices(vertexRowCount);
        RecomputeNormalsFromIndices();
        GenerateMesh();

        MeshId.IsValidOrThrow();
        return new TerrainBatchResult(MeshId, DrawCount);
    }


    public override void Dispose()
    {
        if (MeshId.IsValid())
            Gfx.ResourceContext.Disposer.EnqueueRemoval(MeshId, false);
    }

    private void GenerateMesh()
    {
        ArgumentNullException.ThrowIfNull(_vertices);
        ArgumentNullException.ThrowIfNull(_indices);
        ArgumentOutOfRangeException.ThrowIfLessThan(_vertices.Length, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(_indices.Length, 8);

        var drawCount = _indices.Length;

        var props = MeshDrawProperties.MakeTriElemental(drawCount: drawCount);
        var builder = Gfx.Meshes.StartUploadBuilder(in props);
        builder.UploadVertices<Vertex3D>(_vertices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        builder.UploadIndices<uint>(_indices, BufferUsage.DynamicDraw, BufferStorage.Dynamic,
            BufferAccess.MapWrite);

        var attribBuilder = new VertexAttributeMaker<Vertex3D>();
        builder.AddAttribute(attribBuilder.Make<Vector3>());
        builder.AddAttribute(attribBuilder.Make<Vector2>());
        builder.AddAttribute(attribBuilder.Make<Vector3>());
        builder.AddAttribute(attribBuilder.Make<Vector3>());

        MeshId = builder.Finish();
    }


    /* builder.StartBuilder(DrawPrimitive.Triangles, MeshDrawKind.Elements, DrawElementSize.UnsignedInt);
     builder.CreateVertexBuffer(new GpuVboDescriptor<Vertex3D>
     {
         Data = _vertices, Usage = BufferUsage.DynamicDraw, BindingIndex = 0
     });
     builder.CreateIndexBuffer(new GpuIboDescriptor<uint>() { Data = _indices, Usage = BufferUsage.DynamicDraw });
     var result = builder.BuildMesh(attributes);
*/

    private void GenerateVertex(ReadOnlySpan<byte> heightmap, int vertexRowCount)
    {
        _vertices = new Vertex3D[VertexCount];

        for (int vz = 0; vz < vertexRowCount; vz++)
        {
            int zPix = Math.Min(vz * Step, Size - 1);
            for (int vx = 0; vx < vertexRowCount; vx++)
            {
                int xPix = Math.Min(vx * Step, Size - 1);

                float y = SampleH(heightmap, xPix, zPix);
                var pos = new Vector3(xPix, y, zPix);
                var uv = new Vector2(xPix / (float)(Size - 1), zPix / (float)(Size - 1));

                int vi = vz * vertexRowCount + vx;
                _vertices[vi] = new Vertex3D(pos, uv, Vector3.UnitY, Vector3.UnitX);
            }
        }

        // Gram-Schmidt
        for (int vz = 0; vz < vertexRowCount; vz++)
        {
            for (int vx = 0; vx < vertexRowCount; vx++)
            {
                int vi = vz * vertexRowCount + vx;
                var n = GetNormal(heightmap, vx, vz);
                var t = GetTangent(heightmap, vx, vz, n);
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

                float h0 = _vertices[i0].Position.Y;
                float h1 = _vertices[i1].Position.Y;
                float h2 = _vertices[i2].Position.Y;
                float h3 = _vertices[i3].Position.Y;

                bool diag = MathF.Abs(h0 - h3) <= MathF.Abs(h1 - h2);

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
        // zero normals
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i].Normal = Vector3.Zero;

        // face normals (area-weighted)
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
            var fn = Vector3.Cross(e1, e2); // length ~ 2*area

            v0.Normal += fn;
            v1.Normal += fn;
            v2.Normal += fn;
        }

        // normalize
        for (int i = 0; i < _vertices.Length; i++)
            _vertices[i].Normal = NormalizeSafe(_vertices[i].Normal);
    }


    private float SampleH(ReadOnlySpan<byte> heightmap, int x, int z)
    {
        x = Math.Clamp(x, 0, Size - 1);
        z = Math.Clamp(z, 0, Size - 1);

        const int channels = 4;

        int rowStrideBytes = heightmap.Length / Size;

        int idx = z * rowStrideBytes + x * channels;
        if ((uint)(idx + channels - 1) >= (uint)heightmap.Length)
            return 0f;

        byte r = heightmap[idx];
        return (r / 255f) * MaxHeight;
    }

    private Vector3 GetTangent(ReadOnlySpan<byte> heightmap, int vx, int vz, Vector3 n)
    {
        int xPix = Math.Min(vx * Step, Size - 1);
        int zPix = Math.Min(vz * Step, Size - 1);
        float hL = SampleH(heightmap, xPix - Step, zPix);
        float hR = SampleH(heightmap, xPix + Step, zPix);

        var rawT = new Vector3(2 * Step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }


    private Vector3 GetNormal(ReadOnlySpan<byte> heightmap, int vx, int vz)
    {
        int xPix = Math.Min(vx * Step, Size - 1);
        int zPix = Math.Min(vz * Step, Size - 1);

        float hL = SampleH(heightmap, xPix - Step, zPix);
        float hR = SampleH(heightmap, xPix + Step, zPix);
        float hD = SampleH(heightmap, xPix, zPix - Step);
        float hU = SampleH(heightmap, xPix, zPix + Step);

        var dx = new Vector3(2 * Step, hR - hL, 0f);
        var dz = new Vector3(0f, hU - hD, 2 * Step);

        return NormalizeSafe(Vector3.Cross(dz, dx));
    }

    static Vector3 NormalizeSafe(in Vector3 v)
    {
        float len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : Vector3.UnitY;
    }
}