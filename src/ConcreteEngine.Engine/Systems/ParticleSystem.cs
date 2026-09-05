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

    private int[] _deadIndices;

    internal ParticleSystem(GfxContext gfx)
    {
        if (_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
        _deadIndices = new int[1024];
    }

    internal void Commit()
    {
        if (!_particleManager.HasPendingEmitters) return;

        _particleManager.CommitEmitters();
        
        int max = _deadIndices.Length;
        foreach (var id in _particleManager.GetPendingEmitterIds())
        {
            var emitter = _particleManager.Get(id);
            if (emitter.ParticleCount <= 0) Throwers.InvalidOperation(nameof(emitter.ParticleCount));
            var slot = _particleMesh.CreateParticleMesh(emitter.ParticleCount);
            var meshId = _particleMesh.GetHandle(slot).MeshId;
            emitter.Attach(slot, meshId);
            
            max = int.Max(max, emitter.ParticleCount);
        }
        
        if (max > _deadIndices.Length) _deadIndices = new int[max];

        _particleManager.ClearPendingEmitters();
    }

    
    internal void InterpolateUpload()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
        {
            var emitter = _particleManager.Get(emitterId);
            InterpolateEmitter(emitter.GetEmitterData(), emitter.ParticleCount, timeOffset);
            _particleMesh.UploadGpuData(emitter.BoundSlot, emitter.ParticleCount);
        }
    }

    
    internal void Simulate(float simDt)
    {
        if (_particleManager.EmitterCount == 0) return;

        _processedEmitters.Clear();

        foreach (var it in RenderEcs.Store<EmitterLink>().VisibilityQuery())
        {
            var emitterId = it.Component.EmitterId;
            if (_processedEmitters.Contains(emitterId)) continue;

            var emitter = _particleManager.Get(emitterId);
            if (!emitter.IsAttached) continue;

            var dead = SimulateLife(emitter.GetEmitterData(), emitter.ParticleCount, simDt);
            if (dead > 0) emitter.RespawnParticles(_deadIndices.AsSpan(0, dead));
            SimulateSpatial256(emitter.GetEmitterData(), emitter.ParticleCount, emitter.State.Gravity, simDt);

            _processedEmitters.Add(emitterId);
        }
    }


    [SkipLocalsInit]
    private unsafe void InterpolateEmitter(ParticleEmitterData data, int count, float timeOffset)
    {
        var lifeIndices = data.LifeIndices(count).Ptr;
        ref var start = ref MemoryMarshal.GetArrayDataReference(data.Lut);
        foreach (var it in ParticleEnumerator(data.Velocities(count), data.Positions(count)))
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

    private int SimulateLife(ParticleEmitterData data, int count, float simDt)
    {
        // var lifeStates = data.LifeStates(count).AsSpan();
        // var lifeIndices = data.LifeIndices(count).AsSpan();
        // for (int i = 0; i <= lifeStates.Length - Vector128<int>.Count; i += Vector128<int>.Count) { }

        int index = 0, deadIndex = 0;
        foreach (var it in data.LifeEnumerator(count))
        {
            float life = it.Item1.Life -= simDt;
            if (life > 0)
            {
                var l = float.FusedMultiplyAdd(-life, it.Item1.LifeInvMax, 1f);
                it.Item2 = (byte)float.FusedMultiplyAdd(l, 255f, 0.5f);
                ++index;
            }
            else
            {
                _deadIndices[deadIndex++] = index;
                ++index;
            }
        }

        return deadIndex;
    }

    [SkipLocalsInit]
    private void SimulateSpatial256(ParticleEmitterData emitter, int count, Vector3 gravity, float simDt)
    {
        var gravityStep256 = Vector256.Create(gravity.AsVector128() * simDt);
        var positions = emitter.PositionSpan(count);
        var velocities = emitter.VelocitySpan(count);
        for (int i = 0; i < velocities.Length - 1; i += 2)
        {
            ref var velocity = ref velocities[i];
            var velocity256 = Vector256.Add(
                Vector256.LoadUnsafe(ref Unsafe.As<Vector4, float>(ref velocity)),
                gravityStep256
            );
            velocity256.StoreUnsafe(ref Unsafe.As<Vector4, float>(ref velocity));

            ref var position = ref positions[i];
            var position256 = Vector256.FusedMultiplyAdd(
                velocity256,
                Vector256.Create(simDt),
                Vector256.LoadUnsafe(ref Unsafe.As<Vector4, float>(ref position))
            );
            position256.StoreUnsafe(ref Unsafe.As<Vector4, float>(ref position));
        }
    }

    [SkipLocalsInit]
    private PtrEnumerator<Vector4, Vector4, ParticleVertex> ParticleEnumerator(NativeView<Vector4> velocityView,
        NativeView<Vector4> positionView)
    {
        return new PtrEnumerator<Vector4, Vector4, ParticleVertex>(velocityView, positionView,
            _particleMesh.GetBufferView(velocityView.Length));
    }


    public void Dispose()
    {
        _allocated = false;
        _particleManager.Dispose();
        _particleMesh.Dispose();
    }
}