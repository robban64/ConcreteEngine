using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.TerrainV2;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainManager
{
    //public readonly Terrain Terrain;
    //internal readonly TerrainMeshGenerator TerrainMesh;

    public readonly TerrainNew Terrain;
    public readonly TerrainMesh TerrainMesh;

    public static TerrainManager Instance = null!;

    public TerrainManager(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        // Terrain = new Terrain();
        //TerrainMesh = new TerrainMeshGenerator(gfx);
        
        Terrain = new TerrainNew();
        TerrainMesh = new TerrainMesh(gfx);
        MeshGeneratorRegistry.Instance.Register(TerrainMesh);
        Instance = this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (Terrain.HasHeightmap && TerrainMesh.IboId == default)
        {
            var data = Terrain.Heightmap!.PixelData!.Value.Span;
            TerrainMesh.Allocate(Terrain.GetChunkDict(),data, 257,12);
        }

        /*
        if (!Terrain.IsDirty) return;

        if (Terrain.HasHeightmap && Terrain.MeshId == default)
        {
            var meshId = TerrainMesh.CreateTerrainMesh(Terrain);
            Terrain.MeshId = meshId;
        }
        */
    }
}