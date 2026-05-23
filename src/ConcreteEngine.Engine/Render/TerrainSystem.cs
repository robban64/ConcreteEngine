using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainSystem
{
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

    public void OnTick(AssetSystem assetSystem)
    {
        var t = MainTerrain;

        if (t.Heightmap?.PixelData is not {} heightmap || TerrainMesh.TerrainIboId.IsValid())
            return;

        TerrainMesh.Allocate(t.GetChunks(), heightmap.Span, t.Dimension, t.MaxHeight);
        if (t.Splatmap?.PixelData is {} splatMap)
        {
            TerrainMesh.AllocateFoliage(t, splatMap.Span);
        }

        if (MainTerrain.Material is { } material)
        {
            TextureId arrayTextureId = default;
            var span = material.GetTextureSources();
            var layer = 0;
            foreach (var source in span)
            {
                if(source.Usage != TextureUsage.Environment) continue;

                var textureId = assetSystem.Assets.Get<Texture>(source.Texture).GfxId;
                if (arrayTextureId == default)
                    arrayTextureId = _gfx.Textures.CreateTexture2DArrayFrom(textureId, 4);
                else
                    _gfx.Textures.SetTexture2DArrayLayerFrom(arrayTextureId, textureId, layer++);
            }
        }

    }

}