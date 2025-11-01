#region

using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Worlds.Data;
using ConcreteEngine.Core.Worlds.Entities;
using ConcreteEngine.Core.Worlds.Render;
using ConcreteEngine.Core.Worlds.Render.Batching;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Worlds;

public sealed class WorldTerrain
{
    private const int TerrainHeight = 12;
    private const int TerrainStep = 1;

    public ModelId Model { get; private set; }

    public MaterialId Material { get; private set; }
    private AssetRef<Texture2D> _heightmap;
    private IMeshTable _meshTable;
    private TerrainBatcher _terrain;

    public Transform Transform { get; private set; } = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public WorldTerrain(TerrainBatcher terrain)
    {
        _terrain = terrain;
    }

    internal void AttachModelRegistry(IMeshTable meshTable) => _meshTable = meshTable;

    public bool IsActive => _heightmap.IsValid && _terrain.TextureRef.IsValid && Material > 0;

    public void SetMaterial(MaterialId materialId) => Material = materialId;

    public float GetSmoothHeight(float x, float z)
    {
        int ix = int.Clamp((int)x, 0, _terrain.Dimension);
        int iz = int.Clamp((int)z, 0, _terrain.Dimension);

        float gridSquareSize = _terrain.Dimension / ((float)(_terrain.Size) - 1);
        float xCord = (x % gridSquareSize) / gridSquareSize;
        float zCord = (z % gridSquareSize) / gridSquareSize;
        if (xCord <= 1 - zCord)
        {
            return VectorMath.BarryCentric(
                new Vector3(0, _terrain.GetHeight(ix, iz), 0),
                new Vector3(1, _terrain.GetHeight(ix + 1, iz), 0),
                new Vector3(0, _terrain.GetHeight(ix, iz + 1), 1),
                new Vector2(zCord, xCord));
        }

        return VectorMath.BarryCentric(
            new Vector3(1, _terrain.GetHeight(ix + 1, iz), 0),
            new Vector3(1, _terrain.GetHeight(ix + 1, iz + 1), 1),
            new Vector3(0, _terrain.GetHeight(ix, iz + 1), 1),
            new Vector2(xCord, zCord));
    }

    public void CreateTerrainMesh(Texture2D heightmap)
    {
        ArgumentNullException.ThrowIfNull(heightmap, nameof(heightmap));
        ArgumentOutOfRangeException.ThrowIfEqual(heightmap.PixelData.HasValue, false, nameof(heightmap.PixelData));
        _heightmap = heightmap.RefId;
        _terrain.Initialize(heightmap, TerrainHeight, TerrainStep);
        _terrain.BuildBatch();

        Debug.Assert(_terrain.MeshId > 0);
        Model = _meshTable.CreateModel(_terrain.MeshId, 0, _terrain.DrawCount);
    }
}