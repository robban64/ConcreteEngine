using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainManager
{
    public readonly Terrain Terrain;
    internal readonly TerrainMeshGenerator TerrainMesh;

    public static TerrainManager Instance = null!;

    public TerrainManager(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("TerrainSystem already created");
        Terrain = new Terrain();
        TerrainMesh = new TerrainMeshGenerator(gfx);
        MeshGeneratorRegistry.Instance.Register(TerrainMesh);
        Instance = this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (!Terrain.IsDirty) return;

        if (Terrain.HasHeightmap && Terrain.MeshId == default)
        {
            var meshId = TerrainMesh.CreateTerrainMesh(Terrain);
            Terrain.MeshId = meshId;
        }
    }
}