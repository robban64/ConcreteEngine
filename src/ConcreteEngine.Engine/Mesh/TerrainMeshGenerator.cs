using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Mesh;


public sealed class TerrainMeshGenerator : MeshGenerator
{
    public MeshId MeshId { get; private set; }

    private uint[] _indices = [];
    private Vertex3D[] _vertices = [];
    private readonly Terrain _terrain;

    public int VertexCount { get; private set; }
    public int DrawCount { get; private set; }

    public MaterialId BoundMaterial => _terrain.Material;

    internal TerrainMeshGenerator(GfxContext gfx, Terrain terrain) : base(gfx)
    {
        _terrain = terrain;
    }

    public void CreateTerrainMesh(Texture texture )
    {
        _terrain.CreateTerrainMesh(texture);
        var vertexRowCount = (_terrain.Dimension - 1) / _terrain.Step + 1;
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

    private void GenerateVertex(int vertexRowCount)
    {
        if (_vertices.Length < vertexRowCount * vertexRowCount)
            _vertices = new Vertex3D[VertexCount];

        var vertices = _vertices;
        var step = _terrain.Step;
        var dimension = _terrain.Dimension;
        for (var vz = 0; vz < vertexRowCount; vz++)
        {
            var zPix = Math.Min(vz * step, dimension - 1);
            for (var vx = 0; vx < vertexRowCount; vx++)
            {
                var xPix = Math.Min(vx * step, dimension - 1);

                var y = _terrain.GetHeight(xPix, zPix);
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
                var n = GetNormal(vx, vz, step, dimension);
                var t = GetTangent(vx, vz, step, dimension, n);
                vertices[vi].Normal = n;
                vertices[vi].Tangent = t;
            }
        }
    }

    private void GenerateIndices(int vertexRowCount)
    {
        var quadCount = vertexRowCount - 1;
        var size = quadCount * quadCount * 6;

        if (_indices.Length < size)
            _indices = new uint[quadCount * quadCount * 6];

        var indices = _indices;
        var vertices = _vertices;

        int k = 0;
        for (int z = 0; z < quadCount; z++)
        {
            for (int x = 0; x < quadCount; x++)
            {
                uint i0 = (uint)(z * vertexRowCount + x);
                uint i1 = i0 + 1;
                uint i2 = (uint)((z + 1) * vertexRowCount + x);
                uint i3 = i2 + 1;

                var h0 = vertices[i0].Position.Y;
                var h1 = vertices[i1].Position.Y;
                var h2 = vertices[i2].Position.Y;
                var h3 = vertices[i3].Position.Y;

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

    private Vector3 GetTangent(int vx, int vz, int step, int dimension, Vector3 n)
    {
        var xPix = Math.Min(vx * step, dimension - 1);
        var zPix = Math.Min(vz * step, dimension - 1);
        var hL = _terrain.GetHeight(xPix - step, zPix);
        var hR = _terrain.GetHeight(xPix + step, zPix);

        var rawT = new Vector3(2 * step, hR - hL, 0f);
        var t = rawT - n * Vector3.Dot(rawT, n);
        return NormalizeSafe(t);
    }


    private Vector3 GetNormal(int vx, int vz, int step, int dimension)
    {
        var xPix = Math.Min(vx * step, dimension - 1);
        var zPix = Math.Min(vz * step, dimension - 1);

        var hL = _terrain.GetHeight(xPix - step, zPix);
        var hR = _terrain.GetHeight(xPix + step, zPix);
        var hD = _terrain.GetHeight(xPix, zPix - step);
        var hU = _terrain.GetHeight(xPix, zPix + step);

        var dx = new Vector3(2 * step, hR - hL, 0f);
        var dz = new Vector3(0f, hU - hD, 2 * step);

        return NormalizeSafe(Vector3.Cross(dz, dx));
    }

    private static Vector3 NormalizeSafe(Vector3 v)
    {
        var len2 = v.LengthSquared();
        return len2 > 1e-12f ? v / MathF.Sqrt(len2) : Vector3.UnitY;
    }
}