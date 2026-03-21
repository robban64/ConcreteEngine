using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Worlds;

internal sealed class WorldBundle
{
    public required AnimationTable Animations;
    public required ParticleSystem ParticleSystem;
    public required Terrain Terrain;
    public required Skybox Sky;
    public required MeshGeneratorRegistry MeshRegistry;
}