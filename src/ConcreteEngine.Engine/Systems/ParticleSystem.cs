using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Systems;

internal sealed class ParticleSystem : IDisposable
{
    private static bool _allocated;
    private readonly List<Id16<ParticleEmitter>> _processedEmitters = new(16);

    private readonly ParticleMesh _particleMesh;
    private readonly ParticleManager _particleManager;

    internal ParticleSystem(GfxContext gfx)
    {
        if (_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
    }

    internal void Commit()
    {
        if (_particleManager.HasPendingEmitters)
            CommitPending();

        _particleManager.CommitEmitters();
    }

    private void CommitPending()
    {
        foreach (var id in _particleManager.GetPendingEmitters())
        {
            var emitter = _particleManager.Get(id);
            if (emitter.ParticleCount <= 0) Throwers.InvalidOperation(nameof(emitter.ParticleCount));
            var slot = _particleMesh.CreateParticleMesh(emitter.ParticleCount);
            var meshId = _particleMesh.GetHandle(slot).MeshId;
            emitter.Attach(slot, meshId);
        }

        _particleManager.ClearPendingEmitters();
    }

    public void Dispose()
    {
        _allocated = false;
        _particleManager.Dispose();
        _particleMesh.Dispose();
    }

    internal void Simulate(float simDt)
    {
        if (_particleManager.EmitterCount == 0) return;

        _processedEmitters.Clear();
        foreach (var it in RenderEcs.Store<EmitterLink>().VisibilityQuery())
        {
            if (_processedEmitters.Contains(it.Component.EmitterId)) continue;
            var emitter = _particleManager.Get(it.Component.EmitterId);
            if (!emitter.IsAttached) continue;

            avg.BeginSample();
            emitter.Simulate(simDt);
            avg.EndSample();

            _processedEmitters.Add(it.Component.EmitterId);
        }

        if (avg.Ticks > 40*2) avg.ResetAndPrint("CPU");
    }

    private AvgFrameTimer avg, avg2;

    internal void Execute()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
        {
            var emitter = _particleManager.Get(emitterId);
            avg2.BeginSample();
            ProcessEmitter(emitter.GetEmitterData(), timeOffset);
            avg2.EndSample();
            _particleMesh.UploadGpuData(emitter.BoundSlot, emitter.ParticleCount);
        }

        if (avg2.Ticks > 80*2) avg2.ResetAndPrint("GPU");
    }

    [SkipLocalsInit]
    private unsafe void ProcessEmitter(ParticleEmitterData emitterData, float timeOffset)
    {
        var invLife = emitterData.ParticleInvLifeState.Ptr;
        ref var start = ref MemoryMarshal.GetArrayDataReference(emitterData.Lut);
        foreach (var it in ParticleEnumerator(emitterData.ParticleState))
        {
            var idx = (byte)float.FusedMultiplyAdd(*invLife++, 255f, 0.5f);
            var lut = Unsafe.Add(ref start, idx);

            var pos = it.Item1.Position + it.Item1.Velocity * timeOffset;
            pos.W = lut.Size;
            it.Item2 = new ParticleVertex(in pos, lut.Color);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PtrEnumerator<ParticleState, ParticleVertex> ParticleEnumerator(NativeView<ParticleState> particles) =>
        new(particles, _particleMesh.GetBufferView(particles.Length));
}