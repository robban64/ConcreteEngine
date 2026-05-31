using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainSystem
{
    public bool IsDirty => MainTerrain.IsDirty;

    private readonly GfxContext _gfx;
    public readonly Terrain MainTerrain;
    public readonly TerrainMesh TerrainMesh;

    public static TerrainSystem Instance { get; private set; } = null!;
    public static TerrainSystem Make(GfxContext gfx) => Instance = new TerrainSystem(gfx);

    private TerrainSystem(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        _gfx = gfx;
        MainTerrain = new Terrain();
        Terrain.Main = MainTerrain;

        TerrainMesh = new TerrainMesh(gfx);
    }

    public void OnTick()
    {
        if (!IsDirty) return;
        MainTerrain.IsDirty = false;
        Allocate();
    }

    private void Allocate()
    {
        if (!TerrainMesh.TerrainIboId.IsValid() && MainTerrain.Heightmap?.PixelData is { } heightmap)
        {
            TerrainMesh.Allocate(MainTerrain.GetChunks(), heightmap.Span, MainTerrain.Dimension, MainTerrain.MaxHeight);
            Logger.LogString(LogScope.Engine, "Terrain: allocated terrain");
        }

        if (!TerrainMesh.HasFoliage && MainTerrain.Splatmap?.PixelData is { } splatMap)
        {
            TerrainMesh.AllocateFoliage(MainTerrain, splatMap.Span);
            Logger.LogString(LogScope.Engine, "Terrain: allocated foliage");
        }

        if (MainTerrain.GroundMaterial is { } material)
        {
            if (MainTerrain.GroundAlbedoTextures.IsDirty)
            {
                var textureId = MainTerrain.GroundAlbedoTextures.Compile(_gfx.Textures);
                material.SetOverrideTexture(0, textureId);
                Logger.LogString(LogScope.Engine, "Ground albedo texture changed");
            }
        }

        if (MainTerrain.FoliageTextures.IsDirty && MainTerrain.FoliageMaterial is { } foliageMaterial)
        {
            var textureId = MainTerrain.FoliageTextures.Compile(_gfx.Textures);
            foliageMaterial.SetOverrideTexture(0, textureId);
            Logger.LogString(LogScope.Engine, "Foliage texture changed");
        }
    }
}