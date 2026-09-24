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

    private ushort[] _deadIndices;

    internal ParticleSystem(GfxContext gfx)
    {
        if (_allocated) Throwers.InvalidOperation("ParticleSystem already active");
        _allocated = true;
        _particleMesh = new ParticleMesh(gfx);
        _particleManager = ParticleManager.Instance;
        _deadIndices = new ushort[1024];
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

        if (max > _deadIndices.Length) _deadIndices = new ushort[max];

        _particleManager.ClearPendingEmitters();
    }


    internal void InterpolateUpload()
    {
        var timeOffset = (float)(EngineTime.SimulationDelta * EngineTime.SimulationAlpha);
        foreach (var emitterId in _processedEmitters.AsSpan())
        {
            var emitter = _particleManager.Get(emitterId);
            var data = emitter.GetData();
            _particleMesh.GetBufferView(emitter.AlignedParticleCount, out var positions, out var particles);
            InterpolatePosition(data, positions, timeOffset);
            InterpolateVisual(data, particles);
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

            SimulateEmitter(emitter, simDt);

            _processedEmitters.Add(emitterId);
        }
    }

    private void InterpolatePosition(ParticleEmitterData data, NativeView<Vector4> destination, float timeOffset)
    {
        var destSpan = destination.Reinterpret<float>().AsSpan();
        var velocitiesSpan = data.Velocities.Reinterpret<float>().AsSpan(0, destSpan.Length);
        var positionSpan = data.Positions.Reinterpret<float>().AsSpan(0, destSpan.Length);

        while (destSpan.Length >= Vector256<float>.Count)
        {
            var vPos = Vector256.FusedMultiplyAdd(
                Vector256.Create(velocitiesSpan),
                Vector256.Create(timeOffset),
                Vector256.Create(positionSpan)
            );

            vPos.CopyTo(destSpan);

            destSpan = destSpan.Slice(Vector256<float>.Count);
            positionSpan = positionSpan.Slice(Vector256<float>.Count);
            velocitiesSpan = velocitiesSpan.Slice(Vector256<float>.Count);
        }
    }

    private void InterpolateVisual(ParticleEmitterData data, NativeView<ParticleVertex> destination)
    {
        var destSpan = destination.AsSpan();
        var lifeIndexSpan = data.GetLutIndices(destSpan.Length);
        ref var lutRef = ref data.GetLutRef();
        for (int i = 0; i < destSpan.Length; ++i)
        {
            destSpan[i] = Unsafe.Add(ref lutRef, lifeIndexSpan[i]);
        }
    }


    private void SimulateEmitter(ParticleEmitter emitter, float simDt)
    {
        var count = emitter.AlignedParticleCount;
        var data = emitter.GetData();
        var dead = SimulateLife(data, count, simDt);
        if (dead > 0)
        {
            emitter.RespawnParticles(_deadIndices.AsSpan(0, dead));
        }

        SimulateLifeIndex(data, count);
        SimulateSpatial(data, count, emitter.State.Gravity, simDt);
    }

    private int SimulateLife(ParticleEmitterData data, int count, float simDt)
    {
        int deadIndex = 0;
        ref var deadIndices = ref MemoryMarshal.GetArrayDataReference(_deadIndices);

        var lifeSpan = data.LifeState.AsSpan(0, count);
        for (int i = 0; i <= lifeSpan.Length - Vector256<float>.Count; i += Vector256<float>.Count)
        {
            ref var life = ref lifeSpan[i];
            var vLife = Vector256.Subtract(Vector256.LoadUnsafe(ref life), Vector256.Create(simDt));
            vLife.StoreUnsafe(ref life);

            var mask = Vector256.LessThanOrEqual(vLife, Vector256.Create(0f)).ExtractMostSignificantBits();
            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                Unsafe.Add(ref deadIndices, deadIndex++) = (ushort)(i + p);
                mask &= mask - 1;
            }
        }

        return deadIndex;
    }

    private static void SimulateLifeIndex(ParticleEmitterData data, int count)
    {
        ref var lutIndices = ref MemoryMarshal.GetReference(data.GetLutIndices(count));
        var lifeState = data.LifeState.Slice(0, count).Reinterpret<Vector256<float>>();
        var invMaxLifeState = data.LifeMaxInverse.Slice(0, count).Reinterpret<Vector256<float>>();
        foreach (var it in lifeState.Zip(invMaxLifeState))
        {
            var vDiff = Fma.MultiplyAddNegated(it.Item1, it.Item2, Vector256.Create(1f));
            var vIndex = Fma.MultiplyAdd(vDiff, Vector256.Create(255f), Vector256.Create(0.5f));

            var vIndexInt32 = Avx.ConvertToVector256Int32(vIndex);
            var shorts = Sse2.PackSignedSaturate(vIndexInt32.GetLower(), vIndexInt32.GetUpper());
            var bytes = Sse2.PackUnsignedSaturate(shorts, Vector128<short>.Zero);

            Unsafe.As<byte, long>(ref lutIndices) = bytes.AsInt64().ToScalar();
            lutIndices = ref Unsafe.Add(ref lutIndices, Vector256<float>.Count);
        }
    }

    private static void SimulateSpatial(ParticleEmitterData emitter, int count, Vector3 gravity, float simDt)
    {
        var gravityStep256 = Vector256.Create(gravity.AsVector128() * simDt);

        var velocities = emitter.Velocities.Slice(0, count).Reinterpret<Vector256<float>>();
        var positions = emitter.Positions.Slice(0, count).Reinterpret<Vector256<float>>();

        foreach (var it in velocities.Zip(positions))
        {
            var vVelocity = Vector256.Add(it.Item1, gravityStep256);
            var vPosition = Fma.MultiplyAdd(vVelocity, Vector256.Create(simDt), it.Item2);
            it.Item1 = vVelocity;
            it.Item2 = vPosition;
        }
    }


    public void Dispose()
    {
        _allocated = false;
        _particleManager.Dispose();
        _particleMesh.Dispose();
    }
}