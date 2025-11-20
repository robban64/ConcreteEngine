using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldParticles
{
    public ModelId Model { get; private set; }
    public MaterialId Material { get; private set; }

    private ParticleBatcher _particles;
    private MaterialTable _materialTable;
    private IMeshTable _meshTable;

    internal WorldParticles()
    {
    }


    internal void AttachRenderer(ParticleBatcher batcher, MeshTable meshTable, MaterialTable materialTable)
    {
        _particles = batcher;
        _meshTable = meshTable;
        _materialTable = materialTable;
    }


}