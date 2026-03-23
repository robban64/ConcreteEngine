using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Mesh;

internal sealed class TerrainMeshGenerator : MeshGenerator
{
    public MeshId MeshId { get; private set; }

    private uint[] _indices = [];
    private Vertex3D[] _vertices = [];

    public int VertexCount { get; private set; }
    public int DrawCount { get; private set; }


    internal TerrainMeshGenerator(GfxContext gfx) : base(gfx)
    {
    }


    public MeshId CreateTerrainMesh(Terrain terrain)
    {
        var vertexRowCount = (terrain.Dimension - 1) / terrain.Step + 1;
        VertexCount = vertexRowCount * vertexRowCount;

        GenerateVertex(terrain, vertexRowCount);
        GenerateIndices(vertexRowCount);
        RecomputeNormalsFromIndices();
        GenerateMesh();

        MeshId.IsValidOrThrow();
        return MeshId;
    }


    public override void Dispose()
    {
        if (MeshId.IsValid())
            Gfx.Disposer.EnqueueRemoval(MeshId);
    }


    private void GenerateMesh()
    {
        ArgumentNullException.ThrowIfNull(_vertices);
        ArgumentNullException.ThrowIfNull(_indices);
        ArgumentOutOfRangeException.ThrowIfLessThan(_vertices.Length, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(_indices.Length, 8);

        var drawCount = _indices.Length;

        var props = MeshDrawProperties.MakeElemental(drawCount: drawCount);
        var attribBuilder = new VertexAttributeMaker();

        var meshId = Gfx.Meshes.CreateEmptyMesh(in props, 2, [
            attribBuilder.Make<Vector3>(0), attribBuilder.Make<Vector2>(1),
            attribBuilder.Make<Vector3>(2), attribBuilder.Make<Vector3>(3)
        ]);
        Gfx.Meshes.CreateAttachVertexBuffer(meshId, _vertices, CreateVboArgs.MakeDynamic(0));
        Gfx.Meshes.CreateAttachIndexBuffer(meshId, _indices, CreateIboArgs.MakeDefault());

        MeshId = meshId;
    }

    private void GenerateVertex(Terrain terrain, int vertexRowCount)
    {
        if (_vertices.Length < vertexRowCount * vertexRowCount)
            _vertices = new Vertex3D[VertexCount];

        var vertices = new UnsafeSpan<Vertex3D>(_vertices.AsSpan());
        int step = terrain.Step, dimension = terrain.Dimension;
        for (var vz = 0; vz < vertexRowCount; vz++)
        {
            var zPix = Math.Min(vz * step, dimension - 1);
            for (var vx = 0; vx < vertexRowCount; vx++)
            {
                var xPix = Math.Min(vx * step, dimension - 1);

                var y = terrain.GetHeight(xPix, zPix);
                var pos = new Vector3(xPix, y, zPix);
                var uv = new Vector2(xPix / (float)(dimension - 1), zPix / (float)(dimension - 1));

                var vi = vz * vertexRowCount + vx;
                vertices[vi] = new Vertex3D(pos, uv, Vector3.UnitY, Vector3.UnitX);
            }
        }

        // Gram-Schmidt
        for (var vz = 0; vz < vertexRowCount; vz++)
        {
            for (var vx = 0; vx < vertexRowCount; vx++)
            {
                var vi = vz * vertexRowCount + vx;
                ref var vertex = ref vertices[vi];
                var n = vertex.Normal = GetNormal(terrain, vx, vz, step, dimension);
                vertex.Tangent = GetTangent(terrain, vx, vz, step, dimension, n);
            }
        }
    }

    private void GenerateIndices(int vertexRowCount)
    {
        var quadCount = vertexRowCount - 1;
        var size = quadCount * quadCount * 6;

        if (_indices.Length < size)
            _indices = new uint[quadCount * quadCount * 6];

        var indices = new UnsafeSpan<uint>(_indices.AsSpan());
        var vertices = new UnsafeSpan<Vertex3D>(_vertices.AsSpan());

        int k = 0;
        for (int z = 0; z < quadCount; z++)
        {
            for (int x = 0; x < quadCount; x++)
            {
                uint i0 = (uint)(z * vertexRowCount + x);
                uint i1 = i0 + 1;
                uint i2 = (uint)((z + 1) * vertexRowCount + x);
                uint i3 = i2 + 1;

                var h0 = vertices[(int)i0].Position.Y;
                var h1 = vertices[(int)i1].Position.Y;
                var h2 = vertices[(int)i2].Position.Y;
                var h3 = vertices[(int)i3].Position.Y;

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
        foreach (ref var it in _vertices.AsSpan())
            it.Normal = Vector3.Zero;

        var indices = _indices.AsSpan();
        var len = indices.Length;
        var vertices = new UnsafeSpan<Vertex3D>(_vertices.AsSpan());
        for (int i = 0; i < len; i += 3)
        {
            uint i0 = indices[i + 0], i1 = indices[i + 1], i2 = indices[i + 2];
            ref var v0 = ref vertices[(int)i0];
            ref var v1 = ref vertices[(int)i1];
            ref var v2 = ref vertices[(int)i2];

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
        foreach (ref var it in _vertices.AsSpan())
            it.Normal = NormalizeSafe(it.Normal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetTangent(Terrain terrain, int vx, int vz, int step, int dimension, Vector3 n)
    {
        var xPix = Math.Min(vx * step, dimension - 1);
        var zPix = Math.Min(vz * step, dimension - 1);
        var hL = terrain.GetHeight(xPix - step, zPix);
        var hR = terrain.GetHeight(xPix + step, zPix);

        var rawT = new Vector3(2 * step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetNormal(Terrain terrain, int vx, int vz, int step, int dimension)
    {
        var xPix = Math.Min(vx * step, dimension - 1);
        var zPix = Math.Min(vz * step, dimension - 1);

        var hL = terrain.GetHeight(xPix - step, zPix);
        var hR = terrain.GetHeight(xPix + step, zPix);
        var hD = terrain.GetHeight(xPix, zPix - step);
        var hU = terrain.GetHeight(xPix, zPix + step);

        var dx = new Vector3(2 * step, hR - hL, 0f);
        var dz = new Vector3(0f, hU - hD, 2 * step);

        return NormalizeSafe(Vector3.Cross(dz, dx));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 NormalizeSafe(Vector3 v)
    {
        var len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : Vector3.UnitY;
    }
}