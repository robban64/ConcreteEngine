using ConcreteEngine.Engine.Worlds.Tables;

namespace ConcreteEngine.Engine.Worlds;

internal sealed class WorldBundle
{
    public required Camera Camera;
    public required MeshTable MeshTable;
    public required MaterialTable MaterialTable;
    public required AnimationTable AnimationTable;
    public required ParticleSystem ParticleSystem;
    public required Terrain Terrain;
    public required WorldSky Sky;
}