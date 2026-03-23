using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class TerrainSystem
{
    private GfxContext _gfx;
    private Terrain _terrain;
    private TerrainMeshGenerator _terrainMesh;

    public TerrainSystem(GfxContext gfx, MeshGeneratorRegistry meshRegistry)
    {
        _gfx = gfx;
    }

    public void CreateTerrain(Texture heightmap)
    {
        _terrain.CreateFrom(heightmap);
        
    }
}