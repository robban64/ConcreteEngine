using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds;

internal readonly ref struct WorldContext(WorldEntities entities, WorldSkybox sky, WorldTerrain terrain, WorldParticles particles, MeshTable meshTable, MaterialTable materialTable, AnimationTable animationTable)
{
    public readonly WorldEntities Entities = entities;
    public readonly WorldSkybox Sky = sky;
    public readonly WorldTerrain Terrain = terrain;
    public readonly WorldParticles Particles = particles;
    public readonly MeshTable MeshTable = meshTable;
    public readonly MaterialTable MaterialTable = materialTable;
    public readonly AnimationTable AnimationTable = animationTable;
}
