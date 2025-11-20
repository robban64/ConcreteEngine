using System.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.Render.Batching;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldParticles
{
    // public ModelId Model { get; private set; }
    public MeshId Mesh => _batcher.MeshId;
    public MaterialId Material { get; private set; }

    private ParticleBatcher _batcher;
    private MaterialTable _materialTable;
    private IMeshTable _meshTable;
    
    private ParticleStateData[] _particles = Array.Empty<ParticleStateData>();
    

    internal WorldParticles()
    {
    }

    public bool IsActive => Mesh > 0 && Material > 0;

    public void SetMaterial(MaterialId materialId) => Material = materialId;


    internal void AttachRenderer(ParticleBatcher batcher, MeshTable meshTable, MaterialTable materialTable)
    {
        _batcher = batcher;
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    public void CreateParticleMesh()
    {
        _batcher.BuildBatch();
        _meshTable.CreateModel(_batcher.MeshId, 0, 4, default);
    }

    public void Simulate(float tickDt)
    {
        const float gravity = 9.82f;
        var particles = _particles.AsSpan();
        foreach (ref var particle in particles)
        {
            particle.PrevPosition = particle.Position;
            particle.Velocity += Vector3.One * gravity * tickDt;
            particle.Position += particle.Velocity * tickDt;
        }
    }

    public void PreRenderProcess(float alpha)
    {
        var particles = _particles.AsSpan();
        var gpuParticles = _batcher.GetBufferSpan();
        for (int i = 0; i < particles.Length; i++)
        {
            ref var particle = ref particles[i];
            ref var gpuData = ref gpuParticles[i];
            var newPos = Vector3.Lerp(particle.PrevPosition, particle.Position, alpha);
            gpuData.Position = new Vector4(newPos, 1);
            gpuData.Color = Vector4.One;
        }
        
        _batcher.UploadGpuData();
    }
}