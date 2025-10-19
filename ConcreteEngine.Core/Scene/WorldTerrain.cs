#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Engine.RenderingSystem.Batching;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Scene.Entities;

#endregion

namespace ConcreteEngine.Core.Scene;

public sealed class WorldTerrain
{
    private const int TerrainHeight = 12;
    private const int TerrainStep = 1;

    private MaterialId _materialId;
    private AssetRef<Texture2D> _heightmap;
    private TerrainBatcher _terrain;

    private Transform _transform = new(new(-100, -10, -100), Vector3.One, Quaternion.Identity);

    public WorldTerrain(TerrainBatcher terrain)
    {
        _terrain = terrain;
    }

    public bool IsActive => _terrain.HeightMap != null || _materialId > 0 || _heightmap.Value > 0;

    public void SetMaterial(MaterialId materialId) => _materialId = materialId;

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
        var mesh = new MeshComponent(_terrain.MeshId, _materialId, _terrain.DrawCount);
        EntityUtility.MakeDrawTerrain(in mesh, in _transform, out drawEntity);
    }
}