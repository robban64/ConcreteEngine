using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Tables;

namespace ConcreteEngine.Engine.Worlds;

internal readonly ref struct WorldContext(RenderEntityHub entities, WorldSkybox sky, WorldTerrain terrain, WorldParticles particles, MeshTable meshTable, MaterialTable materialTable, AnimationTable animationTable)
{
    public readonly RenderEntityHub Entities = entities;
    public readonly WorldSkybox Sky = sky;
    public readonly WorldTerrain Terrain = terrain;
    public readonly WorldParticles Particles = particles;
    public readonly MeshTable MeshTable = meshTable;
    public readonly MaterialTable MaterialTable = materialTable;
    public readonly AnimationTable AnimationTable = animationTable;
}
