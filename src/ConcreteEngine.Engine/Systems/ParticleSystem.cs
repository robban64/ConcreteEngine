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
        if (!_particleManager.HasPendingEmitters) return;

        _particleManager.CommitEmitters();
        if (_particleManager.HasPendingEmitters)
            CommitPending();

        _particleManager.ClearPendingEmitters();
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
            emitter.Simulate(simDt);
            _processedEmitters.Add(it.Component.EmitterId);
        }

    }


    internal void Execute()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
        {
            var emitter = _particleManager.Get(emitterId);
            ProcessEmitter(emitter, timeOffset);
            _particleMesh.UploadGpuData(emitter.BoundSlot, emitter.ParticleCount);
        }

    }

    [SkipLocalsInit]
    private unsafe void ProcessEmitter(ParticleEmitter emitter, float timeOffset)
    {
        var lifeIndices = emitter.ParticleLifeIndices.Ptr;
        ref var start = ref MemoryMarshal.GetArrayDataReference(emitter.LutArray);
        foreach(var it in ParticleEnumerator(emitter.Velocities, emitter.Positions))
        {
            var position128 = Vector128.FusedMultiplyAdd(
                Unsafe.BitCast<Vector4, Vector128<float>>(it.Item1), // velocity
                Vector128.Create(timeOffset),
                Unsafe.BitCast<Vector4, Vector128<float>>(it.Item2) // position
            );
            
            position128.StoreUnsafe(ref Unsafe.As<ParticleVertex, float>(ref it.Item3));
            Unsafe.As<float, ParticleLut>(ref it.Item3.Size) = Unsafe.Add(ref start, *lifeIndices++);
        }

    }

    [SkipLocalsInit]
    private PtrEnumerator<Vector4, Vector4, ParticleVertex> ParticleEnumerator(NativeView<Vector4> velocityView, NativeView<Vector4> positionView)
    {
        return new PtrEnumerator<Vector4, Vector4, ParticleVertex>(velocityView, positionView, _particleMesh.GetBufferView(velocityView.Length));
    }
}