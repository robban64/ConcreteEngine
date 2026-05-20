using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainSystem
{
    public readonly Terrain MainTerrain;
    public readonly TerrainMesh TerrainMesh;

    public static TerrainSystem Instance { get; private set; } = null!;
    public static TerrainSystem Make(GfxContext gfx) => Instance = new TerrainSystem(gfx);

    private TerrainSystem(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        MainTerrain = new Terrain();
        Terrain.Main = MainTerrain;

        TerrainMesh = new TerrainMesh(gfx);
    }

    public void OnTick()
    {
        var t = MainTerrain;

        if (t.Heightmap?.PixelData is not {} heightmap || TerrainMesh.TerrainIboId != default)
            return;

        TerrainMesh.Allocate(t.GetChunks(), heightmap.Span, t.Dimension, t.GridDimension, t.MaxHeight);
        if (t.Splatmap?.PixelData is {} splatMap)
        {
            TerrainMesh.AllocateFoliage(t.GetChunks(), splatMap.Span);
        }
    }
}