#region

using System.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.RenderingSystem.Batching;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class WorldTerrain
{
    private const int TerrainHeight = 12;
    private const int TerrainStep = 1;

    private MaterialId _materialId;
    private AssetRef<Texture2D> _heightmap;
    private TerrainBatcher _terrain;

    private Transform _transform = new(Vector3.Zero, Vector3.One, Quaternion.Identity);

    public WorldTerrain(TerrainBatcher terrain)
    {
        _terrain = terrain;
    }

    public bool IsActive => _heightmap.IsValid && _terrain.TextureRef.IsValid && _materialId > 0;

    public void SetMaterial(MaterialId materialId) => _materialId = materialId;

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
    }

    internal void OnPreRender()
    {
        if (!IsActive)
            return;
    }

    internal void GetDrawEntity(out DrawEntity drawEntity)
    {
        var mesh = new ModelComponent(_terrain.MeshId, _materialId, _terrain.DrawCount);
        EntityUtility.MakeDrawTerrain(in mesh, in _transform, out drawEntity);
    }
}