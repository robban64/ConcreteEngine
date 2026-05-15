using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Engine.Render;

public sealed class ParticleSystem : ParticleSystemCore, IDisposable
{
    public static new ParticleSystem Instance { get; private set; } = null!;
    public static ParticleSystem Make(GfxContext gfx) => Instance = new ParticleSystem(gfx);

    private readonly ParticleMesh _particleMesh;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private readonly Dictionary<string, ParticleEmitter> _byName = new(4);

    private ParticleSystem(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("ParticleSystem already created");
        _particleMesh = new ParticleMesh(gfx);
    }

    public override ParticleEmitter CreateEmitter(string name, int particleCount, in EmitterSpatialParams definition,
        in EmitterVisualParams visualParams)
    {
        if (_byName.ContainsKey(name)) throw new InvalidOperationException();

        var emitterId = new Id32<ParticleEmitter>(_emitters.Count + 1);
        var slot = _particleMesh.CreateParticleMesh(particleCount);
        var emitter = new ParticleEmitter(name, emitterId, slot, particleCount, in definition, in visualParams);

        if (_emitters.Count > 0 && GetEmitterOrNull(emitterId) != null)
            throw new InvalidOperationException();

        _emitters.Add(emitter);
        _byName[name] = emitter;


        return emitter;
    }

    public override ReadOnlySpan<ParticleEmitter> GetEmitters() => CollectionsMarshal.AsSpan(_emitters);

    public override bool TryGetEmitter(string name, out ParticleEmitter emitter) =>
        _byName.TryGetValue(name, out emitter!);

    public override ParticleEmitter GetEmitter(Id32<ParticleEmitter> emitterId)
    {
        var emitter = GetEmitterOrNull(emitterId);
        if (emitter is null) Throwers.NotFoundBy("Missing emitter emitterId", emitterId);
        return emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ParticleEmitter? GetEmitterOrNull(Id32<ParticleEmitter> emitterId)
    {
        var index = emitterId.Index();
        if ((uint)index >= (uint)_emitters.Count) return null;

        var emitter = _emitters[index];
        if (emitter != null! && emitter.Id == emitterId)
            return emitter;

        var foundIndex = SearchMethod.BinarySearchManaged(GetEmitters(), emitterId.Value, out emitter);
        return foundIndex == -1 ? null : emitter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override MeshId GetEmitterMesh(ParticleEmitter emitter)
    {
        return _particleMesh.GetHandle(emitter.Slot).MeshId;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void GetMeshWriteData(ParticleEmitter emitter, out NativeView<ParticleGpuInstance> gpuView,
        out NativeView<ParticleCpuInstance> cpuView)
    {
        cpuView = emitter.GetParticleView();
        gpuView = _particleMesh.GetBufferView(emitter.ParticleCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UploadEmitter(ParticleEmitter emitter)
    {
        _particleMesh.UploadGpuData(emitter.Slot, emitter.ParticleCount);
    }

    public void Dispose()
    {
        foreach (var emitter in _emitters)
            emitter.Dispose();
    }

    internal static void SimulateEmitter(ParticleEmitter emitter, float simDt)
    {
        if (emitter.Seed == 0) emitter.NewSeed();

        var gravityStep = emitter.SpatialParams().Gravity * simDt;
        var translation = emitter.Translation;
        var direction = emitter.Direction;
        var rng = new FastRandom(emitter.Seed);

        var particles = emitter.GetParticleView();
        for (int i = 0; i < particles.Length; i++)
        {
            ref var p = ref particles[i];
            if (p.Life <= 0)
            {
                rng = RespawnParticle(ref p, in emitter.SpatialParams(), translation, direction, rng);
                continue;
            }

            p.Life -= simDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * simDt;
        }

        emitter.Seed = rng.Seed;
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FastRandom RespawnParticle(ref ParticleCpuInstance p, in EmitterSpatialParams param,
        Vector3 translation,
        Vector3 direction, FastRandom rng)
    {
        rng.IncrementSeed();

        var randDir = new Vector3(rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1));
        var speed = rng.RandomFloat(param.SpeedMinMax);
        var spread = new Vector2(-param.Spread, param.Spread);

        p.Position = translation +
                     new Vector3(rng.RandomFloat(spread), rng.RandomFloat(spread), rng.RandomFloat(spread));

        p.Velocity = Vector3.Normalize(direction + randDir * 0.5f) * speed;

        p.MaxLife = rng.RandomFloat(param.LifeMinMax);
        p.Life = p.MaxLife;
        return rng;
    }
}