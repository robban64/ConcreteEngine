#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldParticles
{
    // public ModelId Model { get; private set; }
    public MaterialId Material { get; private set; }

    private ParticleMeshGenerator _particleGenerator;
    private MaterialTable _materialTable;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private int _handleHigh = 0;

    internal ReadOnlySpan<ParticleEmitter> EmitterSpan => CollectionsMarshal.AsSpan(_emitters);

    internal WorldParticles()
    {
    }

    internal void AttachRenderer(ParticleMeshGenerator meshGenerator, MaterialTable materialTable)
    {
        _particleGenerator = meshGenerator;
        _materialTable = materialTable;
    }

    public void SetMaterial(MaterialId materialId) => Material = materialId;

    internal ParticleEmitter GetEmitter(int emitterHandle)
    {
        if (emitterHandle > _handleHigh) throw new IndexOutOfRangeException();

        if (emitterHandle < _emitters.Count)
        {
            var found = _emitters[emitterHandle];
            if (found.EmitterHandle == emitterHandle)
                return found;
        }

        var index = SortMethod.BinarySearch(_emitters, emitterHandle);
        if (index < 0)
            throw new InvalidOperationException($"Missing emitter handle {emitterHandle}");

        return _emitters[index];
    }

    public ParticleEmitter CreateEmitter(string name, int particleCount, in ParticleDefinition definition)
    {
        var slotHandle = _particleGenerator.CreateParticleMesh(particleCount, out var mesh);
        var emitter = new ParticleEmitter(name, slotHandle, particleCount, in definition)
        {
            MeshId = mesh, MaterialId = Material
        };
        _emitters.Add(emitter);
        _handleHigh = int.Max(_handleHigh, slotHandle);
        return emitter;
    }

    internal ParticleMeshWriter GetMeshWriterFor(ParticleEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        return _particleGenerator.GetWriteBuffer(emitter.EmitterHandle, emitter.ParticleCount);
    }


    private FrameProfileTimer _timer = new(40, 25);

    public void UpdateSimulate(WorldEntities entities, float fixedDt)
    {
        SimulateEmitters(fixedDt);
        UpdateEntities(entities);
    }


    private void UpdateEntities(WorldEntities entities)
    {
        BoundingBox currBox = default;
        int prevHandle = -1;

        var core = entities.Core;
        foreach (var query in entities.Query<ParticleComponent>())
        {
            var handle = query.Component.EmitterHandle;
            ref var box = ref core.GetBoxById(query.Entity);
            if (prevHandle != handle)
            {
                var emitter = GetEmitter(query.Component.EmitterHandle);
                emitter.UpdateLocalBounds(out currBox);
                prevHandle = emitter.EmitterHandle;
            }

            box.Bounds = currBox;
        }
    }

    private void SimulateEmitters(float fixedDt)
    {
        foreach (var emitter in _emitters)
        {
            if (emitter.State.Seed == 0) emitter.NewSeed();
            Simulate(emitter, fixedDt);
        }
    }

    private static void Simulate(ParticleEmitter emitter, float fixedDt)
    {
        ref var state = ref emitter.State;
        ref readonly var def = ref emitter.Definition;

        var gravityStep = def.Gravity * fixedDt;
        var speedMinMax = def.SpeedMinMax;
        var lifeMinMax = def.LifeMinMax;

        var particles = emitter.Particles;
        var len = particles.Length;

        for (var i = 0; i < len; i++)
        {
            ref var p = ref particles[i];
            if (p.Life <= 0)
            {
                RespawnParticle(ref p, ref state, speedMinMax, lifeMinMax);
                continue;
            }

            p.Life -= fixedDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * fixedDt;
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RespawnParticle(ref ParticleStateData p, ref ParticleEmitterState state, Vector2 speedMinMax,
            Vector2 lifeMinMax)
        {
            var rng = new FastRandom(state.NextSeed());

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

            var speed = rng.RandomFloat(speedMinMax);
            p.Velocity = Vector3.Normalize(state.Direction + randDir * 0.5f) * speed;

            p.MaxLife = rng.RandomFloat(lifeMinMax);
            p.Life = p.MaxLife;
        }
    }
}