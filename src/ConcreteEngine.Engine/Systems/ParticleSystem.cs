using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Components;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

internal sealed class ParticleSystem : IDisposable
{
    private static bool _allocated;

    private readonly ParticleMesh _particleMesh;
    private readonly ParticleManager _particleManager;

    private readonly List<Id16<ParticleEmitter>> _processedEmitters = new(16);

    internal ParticleSystem(GfxContext gfx)
    {
        if (_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
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

    internal void Simulate(float simDt)
    {
        if (_particleManager.EmitterCount == 0) return;

        _processedEmitters.Clear();

        foreach (var it in RenderWorld.Queries.VisibilityQuery<EmitterLink>())
        {
            var emitterId = it.Component.EmitterId;
            if (_processedEmitters.Contains(emitterId)) continue;

            var emitter = _particleManager.Get(emitterId);
            if (!emitter.IsAttached) continue;

            emitter.Simulate(simDt);
            _processedEmitters.Add(emitterId);
        }

    }

    internal void Execute()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
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
}