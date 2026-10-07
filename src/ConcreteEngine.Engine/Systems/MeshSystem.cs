using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;
/*
internal sealed class MeshSystem : IDisposable
{
    public readonly ParticleMesh _particleMesh;

    internal MeshSystem(GfxContext gfx)
    {
        _particleMesh = new ParticleMesh(gfx);
    }

    internal void BindParticleMesh(int count)
    {
        var slot = _particleMesh.CreateParticleMesh(count);
        var meshId = _particleMesh.GetHandle(slot).MeshId;
    }

    internal void Commit()
    {
        if (!_particleManager.HasPendingEmitters) return;

        _particleManager.CommitEmitters();

        foreach (var id in _particleManager.GetPendingEmitterIds())
        {
            var emitter = _particleManager.Get(id);
            if (emitter.ParticleCount <= 0) Throwers.InvalidOperation(nameof(emitter.ParticleCount));
            var slot = _particleMesh.CreateParticleMesh(emitter.ParticleCount);
            var meshId = _particleMesh.GetHandle(slot).MeshId;
            emitter.Attach(slot, meshId);
        }

        _particleManager.ClearPendingEmitters();
    }

    internal void Execute()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        
        foreach (var emitterId in _particleManager.GetProcessedEmitterIds())
        {
            var emitter = _particleManager.Get(emitterId);
            _particleMesh.GetBufferView(emitter.AlignedParticleCount, out var positions, out var particles);

            var emitterData = emitter.GetData();
            emitterData.InterpolatePosition(positions, timeOffset);
            emitterData.InterpolateVisual(particles);
            
            _particleMesh.UploadGpuData(emitter.BoundSlot, emitter.ParticleCount);
        }

    }


    public void Dispose()
    {
        _allocated = false;
        _particleManager.Dispose();
        _particleMesh.Dispose();
    }
}*/