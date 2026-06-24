using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

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

    public void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, CameraFrustum camera)
    {
        var material = MainTerrain.MaterialId;
        var foliageMaterial = MainTerrain.FoliageMaterialId;

        foreach (var it in TerrainMesh.GetMeshChunks())
        {
            if (!camera.IntersectsBox(in MainTerrain.GetChunk(it.Slot).GetBounds())) continue;
            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(it.TerrainMeshId, material);
            commandBuffer.SubmitIdentity(cmd, meta);

            if (it.FoliageCount > 0)
            {
                meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Transparent);
                cmd = new DrawCommand(it.FoliageMeshId, foliageMaterial, instanceCount: (uint)it.FoliageCount);
                commandBuffer.SubmitIdentity(cmd, meta);
            }
        }
    }

    private void Allocate()
    {
        if (!TerrainMesh.TerrainIboId.IsValid() && MainTerrain.Heightmap?.TryGetPixelSpan(out var heightData) == true)
        {
            TerrainMesh.Allocate(MainTerrain.GetChunks(), heightData, MainTerrain.Dimension, MainTerrain.MaxHeight);
            Logger.LogString(LogScope.Engine, "Terrain: allocated terrain");
        }

        if (!TerrainMesh.HasFoliage && MainTerrain.Splatmap?.TryGetPixelSpan(out var splatMapData) == true)
        {
            TerrainMesh.AllocateFoliage(MainTerrain, splatMapData);
            Logger.LogString(LogScope.Engine, "Terrain: allocated foliage");
        }

        if (MainTerrain.GroundMaterial is { } material)
        {
            if (MainTerrain.GroundAlbedoTextures.IsDirty)
            {
                var textureId = MainTerrain.GroundAlbedoTextures.Compile(_gfx.Textures);
                material.SetSourceSlot(0, AssetId.Empty, textureId);
                Logger.LogString(LogScope.Engine, "Ground albedo texture changed");
            }
        }

        if (MainTerrain.FoliageTextures.IsDirty && MainTerrain.FoliageMaterial is { } foliageMaterial)
        {
            var textureId = MainTerrain.FoliageTextures.Compile(_gfx.Textures);
            foliageMaterial.SetSourceSlot(0, AssetId.Empty, textureId);
            Logger.LogString(LogScope.Engine, "Foliage texture changed");
        }
    }
}