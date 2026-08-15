using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Terrains;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Systems;

internal sealed class TerrainSystem
{
    public bool IsDirty => MainTerrain?.IsDirty ?? false;

    private readonly GfxContext _gfx;
    public readonly TerrainMesh TerrainMesh;

    public readonly Terrain MainTerrain;

    internal TerrainSystem(GfxContext gfx)
    {
        _gfx = gfx;
        MainTerrain = new Terrain();
        Terrain.Main = MainTerrain;

        TerrainMesh = new TerrainMesh(gfx);
    }

    public void Commit()
    {
        if (!MainTerrain.IsDirty) return;
        MainTerrain.IsDirty = false;
        Allocate();
    }

    private void OnAllocateTerrain()
    {
        var terrainMat = MainTerrain.MaterialId;
        foreach (var it in TerrainMesh.GetMeshChunks())
        {
            var chunk = MainTerrain.GetChunk(it.Slot);

            var source = new RenderSource(it.TerrainMeshId, terrainMat);
            var drawPolicy = new DrawPolicy(DrawQueue.Terrain, PassMask.Default);
            var entity = RenderEcs.Core.AddEntity(source, drawPolicy);
            RenderEcs.Core.GetWorldBounds(entity) = new BoundingAxisBox(in chunk.GetBounds());
        }
    }

    private void OnAllocateFoliage()
    {
        var mat = MainTerrain.FoliageMaterialId;
        foreach (var it in TerrainMesh.GetMeshChunks())
        {
            var chunk = MainTerrain.GetChunk(it.Slot);

            var source = new RenderSource(it.FoliageMeshId, mat, flags: EntityDrawFlags.Instanced);
            var drawPolicy = new DrawPolicy(DrawQueue.Transparent, PassMask.Default);
            var entity = RenderEcs.Core.AddEntity(source, drawPolicy);
            RenderEcs.Core.GetWorldBounds(entity) = new BoundingAxisBox(in chunk.GetBounds());

            var component = new DrawInstancedComponent { Instances = (uint)it.FoliageCount };
            RenderEcs.Store<DrawInstancedComponent>().Add(entity, component);
        }
    }

    private void Allocate()
    {
        if (!TerrainMesh.TerrainIboId.IsValid() && MainTerrain.Heightmap?.TryGetPixelSpan(out var heightData) == true)
        {
            TerrainMesh.Allocate(MainTerrain.GetChunks(), heightData, MainTerrain.Dimension, MainTerrain.MaxHeight);
            OnAllocateTerrain();
            Logger.Message("Terrain: allocated terrain");
        }

        if (!TerrainMesh.HasFoliage && MainTerrain.Splatmap?.TryGetPixelSpan(out var splatMapData) == true)
        {
            TerrainMesh.AllocateFoliage(MainTerrain, splatMapData);
            OnAllocateFoliage();
            Logger.Message("Terrain: allocated foliage");
        }

        if (MainTerrain.GroundMaterial is { } material)
        {
            if (MainTerrain.GroundAlbedoTextures.IsDirty)
            {
                var textureId = MainTerrain.GroundAlbedoTextures.Compile(_gfx.Textures);
                material.SetSourceSlot(textureId, SamplerSlot.Diffuse, SamplerProfile.AnisotropicWrap);
                Logger.Message("Ground albedo texture changed");
            }
        }

        if (MainTerrain.FoliageTextures.IsDirty && MainTerrain.FoliageMaterial is { } foliageMaterial)
        {
            var textureId = MainTerrain.FoliageTextures.Compile(_gfx.Textures);
            foliageMaterial.SetSourceSlot(textureId, SamplerSlot.Diffuse, SamplerProfile.AnisotropicClamp);
            Logger.Message("Foliage texture changed");
        }
    }
}