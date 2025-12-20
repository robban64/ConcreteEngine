using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;

public sealed class Terrain
{
    private const int TerrainHeight = 12;
    private const int TerrainStep = 1;

    public ModelId Model { get; private set; }

    public MaterialId Material { get; private set; }
    internal TerrainMeshGenerator MeshGenerator { get; private set; }

    private AssetRef<Texture2D> _heightmap;

    private MaterialTable _materialTable;
    private readonly MeshTable _meshTable;


    internal Terrain(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public bool IsActive => _heightmap.IsValid && MeshGenerator.TextureRef.IsValid && Material > 0;
    public void SetMaterial(MaterialId materialId) => Material = materialId;

    internal void AttachRenderer(TerrainMeshGenerator meshGenerator)
    {
        MeshGenerator = meshGenerator;
    }

    public void CreateTerrainMesh(Texture2D heightmap)
    {
        ArgumentNullException.ThrowIfNull(heightmap);
        ArgumentOutOfRangeException.ThrowIfEqual(heightmap.PixelData.HasValue, false, nameof(heightmap.PixelData));
        _heightmap = heightmap.RefId;
        MeshGenerator.Initialize(heightmap, TerrainHeight, TerrainStep);
        MeshGenerator.BuildBatch();

        Debug.Assert(MeshGenerator.MeshId > 0);

        var bounds = new BoundingBox(Vector3.Zero, new Vector3(MeshGenerator.Dimension, TerrainHeight, MeshGenerator.Dimension));
        Model = _meshTable.CreateSimpleModel(MeshGenerator.MeshId, 0, MeshGenerator.DrawCount, in bounds);
    }

    public float GetHeight(int x, int z) => MeshGenerator.GetHeight(x, z);

    public float GetSmoothHeight(float x, float z)
    {
        var terrain = MeshGenerator;
        var ix = int.Clamp((int)x, 0, terrain.Dimension);
        var iz = int.Clamp((int)z, 0, terrain.Dimension);

        float gridSquareSize = terrain.Dimension / ((float)terrain.Size - 1);
        float xCord = x % gridSquareSize / gridSquareSize;
        float zCord = z % gridSquareSize / gridSquareSize;
        if (xCord <= 1 - zCord)
        {
            return VectorMath.BarryCentric(
                new Vector3(0, terrain.GetHeight(ix, iz), 0),
                new Vector3(1, terrain.GetHeight(ix + 1, iz), 0),
                new Vector3(0, terrain.GetHeight(ix, iz + 1), 1),
                new Vector2(zCord, xCord));
        }

        return VectorMath.BarryCentric(
            new Vector3(1, terrain.GetHeight(ix + 1, iz), 0),
            new Vector3(1, terrain.GetHeight(ix + 1, iz + 1), 1),
            new Vector3(0, terrain.GetHeight(ix, iz + 1), 1),
            new Vector2(xCord, zCord));
    }

    public Vector3 GetPointOnTerrainPlane(in Ray ray)
    {
        float maxHeight = MeshGenerator.MaxHeight;
        var size = MeshGenerator.Dimension;

        var n = Vector3.UnitY;
        Vector3 p0 = default;

        var numerator = Vector3.Dot(p0 - ray.Position, n);
        var denominator = Vector3.Dot(ray.Direction, n);

        if (Math.Abs(denominator) < 1e-6f)
            return Vector3.Zero;

        var t = numerator / denominator;
        if (t < 0) return Vector3.Zero;
        var pointOnPlane = ray.GetPointOnRay(t);

        if (pointOnPlane.X < 0 || pointOnPlane.Z < 0 || pointOnPlane.X > size || pointOnPlane.Z > size)
            return Vector3.Zero;

        var terrainHeight = GetSmoothHeight(pointOnPlane.X, pointOnPlane.Z);

        if (Math.Abs(pointOnPlane.Y - terrainHeight) > maxHeight)
            return Vector3.Zero;

        return pointOnPlane with { Y = terrainHeight };
    }
}