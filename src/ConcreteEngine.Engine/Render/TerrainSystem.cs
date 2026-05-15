using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainSystem
{
    public readonly Terrain Terrain;
    public readonly TerrainMesh TerrainMesh;

    public static TerrainSystem Instance = null!;

    public TerrainSystem(TerrainMesh terrainMesh)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        Terrain = new Terrain();
        TerrainMesh = terrainMesh;
        Instance = this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (Terrain.HasHeightmap && TerrainMesh.IboId == default)
        {
            var t = Terrain;
            var data = t.Heightmap!.PixelData!.Value.Span;
            TerrainMesh.Allocate(Terrain.GetChunks(), data, t.Dimension, t.GridDimension, t.MaxHeight);
        }
    }
}