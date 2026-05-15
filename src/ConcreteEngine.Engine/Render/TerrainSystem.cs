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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (MainTerrain.HasHeightmap && TerrainMesh.IboId == default)
        {
            var t = MainTerrain;
            var data = t.Heightmap!.PixelData!.Value.Span;
            TerrainMesh.Allocate(MainTerrain.GetChunks(), data, t.Dimension, t.GridDimension, t.MaxHeight);
        }
    }
}