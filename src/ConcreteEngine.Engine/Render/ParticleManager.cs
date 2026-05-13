using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Engine.Render;

public sealed class ParticleManager : IDisposable
{
    public static ParticleManager Instance { get; private set; } = null!;
    
    private readonly ParticleMeshGenerator _particleGenerator;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private readonly Dictionary<string, ParticleEmitter> _byName = new(4);

    internal ParticleManager(GfxContext gfx)
    {
        if (Instance is not null) throw new InvalidOperationException("ParticleSystem already created");
        _particleGenerator = MeshGeneratorRegistry.Instance.Register(new ParticleMeshGenerator(gfx));
        Instance = this;
    }
    
    public ParticleEmitter CreateEmitter(string name, int particleCount, in EmitterSpatialParams definition,
        in ParticleState state, in EmitterVisualParams visualParams)
    {
        if (_byName.ContainsKey(name)) throw new InvalidOperationException();

        var emitterId = new Id32<ParticleEmitter>(_emitters.Count + 1);
        var slot = _particleGenerator.CreateParticleMesh(particleCount);
        var emitter = new ParticleEmitter(name, emitterId, slot, particleCount, in definition, in state, in visualParams);

        if (_emitters.Count > 0 && GetEmitterOrNull(emitterId) != null)
            throw new InvalidOperationException();

        _emitters.Add(emitter);
        _byName[name] = emitter;
        

        return emitter;
    }
    
    public ReadOnlySpan<ParticleEmitter> GetEmitters() => CollectionsMarshal.AsSpan(_emitters);

    public bool TryGetEmitter(string name, out ParticleEmitter emitter) => _byName.TryGetValue(name, out emitter!);
    
    public ParticleEmitter GetEmitter(Id32<ParticleEmitter> emitterId)
    {
        var emitter = GetEmitterOrNull(emitterId);
        if(emitter is null) Throwers.NotFoundBy("Missing emitter emitterId", emitterId);
        return emitter;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParticleEmitter? GetEmitterOrNull(Id32<ParticleEmitter> emitterId)
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
    public MeshId GetEmitterMesh(ParticleEmitter emitter)
    {
        return _particleGenerator.GetHandle(emitter.Slot).MeshId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ParticleMeshWriter GetMeshWriterFor(ParticleEmitter emitter)
    {
        var emitterParticles = emitter.GetParticleView();
        var gpuParticles = _particleGenerator.GetBufferView(emitter.ParticleCount);
        return new ParticleMeshWriter(emitter.Slot, in emitter.VisualParams(), gpuParticles, emitterParticles);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UploadWriter(in ParticleMeshWriter writer)
    {
        _particleGenerator.UploadGpuData(writer.Slot, writer.Length);
    }

    internal void UpdateSimulate(float fixedDt)
    {
        foreach (var emitter in CollectionsMarshal.AsSpan(_emitters))
        {
            if (emitter.Seed == 0) emitter.NewSeed();
            SimulateEmitters(emitter, fixedDt);
        }
    }

    private static void SimulateEmitters(ParticleEmitter emitter, float fixedDt)
    {
        var gravityStep = emitter.SpatialParams().Gravity * fixedDt;
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

            p.Life -= fixedDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * fixedDt;
        }

        emitter.Seed = rng.Seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FastRandom RespawnParticle(ref ParticleStateData p, in EmitterSpatialParams def, Vector3 translation, Vector3 direction, FastRandom rng)
    {
        rng.IncrementSeed();

        var randDir = new Vector3(rng.RandomFloat(-1,1),rng.RandomFloat(-1,1),rng.RandomFloat(-1,1));
        var speed = rng.RandomFloat(def.SpeedMinMax);
        var spread = new Vector2(-def.Spread, def.Spread);

        p.Position = translation + new Vector3(
            rng.RandomFloat(spread),
            rng.RandomFloat(spread),
            rng.RandomFloat(spread));

        p.Velocity = Vector3.Normalize(direction + randDir * 0.5f) * speed;

        p.MaxLife = rng.RandomFloat(def.LifeMinMax);
        p.Life = p.MaxLife;
        return rng;
    }

    public void Dispose()
    {
        foreach (var emitter in _emitters)
            emitter.Dispose();
    }
}