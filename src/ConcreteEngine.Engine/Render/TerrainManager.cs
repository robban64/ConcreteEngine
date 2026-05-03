using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainManager
{
    public readonly Terrain Terrain;
    public readonly TerrainMesh TerrainMesh;

    public static TerrainManager Instance = null!;

    public TerrainManager(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        Terrain = new Terrain();
        TerrainMesh = new TerrainMesh(gfx);
        MeshGeneratorRegistry.Instance.Register(TerrainMesh);
        Instance = this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (Terrain.HasHeightmap && TerrainMesh.IboId == default)
        {
            var t = Terrain;
            var data = t.Heightmap!.PixelData!.Value.Span;
            TerrainMesh.Allocate(Terrain.GetChunks(),data, t.Dimension, t.GridDimension,t.MaxHeight);
        }
    }
}