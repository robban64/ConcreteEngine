using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Worlds.Mesh;

namespace ConcreteEngine.Engine.Worlds;

public sealed class ParticleSystem
{
    private MaterialId Material { get; set; }

    private ParticleMeshGenerator _particleGenerator = null!;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private readonly Dictionary<string, ParticleEmitter> _byName = new(4);

    internal ParticleSystem()
    {
    }

    internal ReadOnlySpan<ParticleEmitter> GetEmitters() => CollectionsMarshal.AsSpan(_emitters);

    internal void AttachRenderer(ParticleMeshGenerator meshGenerator)
    {
        _particleGenerator = meshGenerator;
    }

    public void SetMaterial(MaterialId materialId) => Material = materialId;


    public bool TryGetEmitter(string name, out ParticleEmitter emitter) => _byName.TryGetValue(name, out emitter!);

    public ParticleEmitter? GetEmitterOrNull(int handle)
    {
        var index = handle - 1;
        if ((uint)index >= _emitters.Count) return null;

        var emitter = _emitters[index];
        if (emitter != null! && emitter.EmitterHandle == handle)
            return _emitters[index];

        var foundIndex = SortMethod.BinarySearchBy(CollectionsMarshal.AsSpan(_emitters), handle, out emitter);
        return foundIndex == -1 ? null : emitter;
    }

    public ParticleEmitter GetEmitter(int handle)
    {
        var index = handle - 1;
        if ((uint)index >= _emitters.Count) throw new ArgumentOutOfRangeException(nameof(handle));

        var emitter = _emitters[index];
        if (emitter != null! && emitter.EmitterHandle == handle)
            return _emitters[index];

        var foundIndex = SortMethod.BinarySearchBy(CollectionsMarshal.AsSpan(_emitters), handle, out var result);
        if (foundIndex < 0 || result == null!)
            throw new InvalidOperationException($"Missing emitter handle {handle}");

        return result;
    }

    public ParticleEmitter CreateEmitter(string name, int particleCount, in ParticleDefinition definition,
        in ParticleState state)
    {
        if (_byName.ContainsKey(name)) throw new InvalidOperationException();

        var slot = _particleGenerator.CreateParticleMesh(particleCount, out var mesh);
        var handle = slot + 1;
        var emitter = new ParticleEmitter(name, handle, mesh, particleCount, in definition, in state);

        if (_emitters.Count > 0 && GetEmitterOrNull(handle) != null)
            throw new InvalidOperationException();

        _emitters.Add(emitter);
        _byName[name] = emitter;

        return emitter;
    }

    internal ParticleMeshWriter GetMeshWriterFor(ParticleEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        return _particleGenerator.GetWriteBuffer(emitter);
    }

    internal void UpdateSimulate(float fixedDt)
    {
        foreach (var emitter in CollectionsMarshal.AsSpan(_emitters))
        {
            if (emitter.State.Seed == 0) emitter.NewSeed();
            SimulateEmitters(emitter, fixedDt);
        }
/*
        var core = Ecs.Render.Core;
        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var emitter = GetEmitter(query.Component.Emitter);
            emitter.OriginTranslation = core.GetTransform(query.RenderEntity).Transform.Translation;
        }
        */
    }

    private static void SimulateEmitters(ParticleEmitter emitter, float fixedDt)
    {
        var gravityStep = emitter.Definition.Gravity * fixedDt;
        var particles = emitter.GetParticleData();

        foreach (ref var p in particles)
        {
            if (p.Life <= 0)
            {
                emitter.State.NextSeed();
                RespawnParticle(ref p, ref emitter.GetState(), in emitter.GetDefinition());
                continue;
            }

            p.Life -= fixedDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * fixedDt;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RespawnParticle(ref ParticleStateData p, ref ParticleState state, in ParticleDefinition def)
    {
        var rng = new FastRandom(state.Seed);
        var spread = new Vector2(-state.Spread, state.Spread);
        var rndMinMax = new Vector2(-1, 1);

        p.Position = state.Translation + new Vector3(
            rng.RandomFloat(spread),
            rng.RandomFloat(spread),
            rng.RandomFloat(spread));

        var randDir = new Vector3(
            rng.RandomFloat(rndMinMax),
            rng.RandomFloat(rndMinMax),
            rng.RandomFloat(rndMinMax));

        var speed = rng.RandomFloat(def.SpeedMinMax);
        p.Velocity = Vector3.Normalize(state.Direction + randDir * 0.5f) * speed;

        p.MaxLife = rng.RandomFloat(def.LifeMinMax);
        p.Life = p.MaxLife;
    }
}