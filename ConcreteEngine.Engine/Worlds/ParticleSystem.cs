using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Identity;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Mesh;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Engine.Worlds;

public sealed class ParticleSystem
{
    // public ModelId Model { get; private set; }

    private int _handleHigh = 0;

    private MaterialId Material { get; set; }

    private ParticleMeshGenerator _particleGenerator = null!;
    private readonly MaterialTable _materialTable;
    private readonly MeshTable _meshTable;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private readonly Dictionary<string, ParticleEmitter> _byName = new(4);

    internal ParticleSystem(MeshTable meshTable, MaterialTable materialTable)
    {
        _meshTable = meshTable;
        _materialTable = materialTable;
    }

    internal ReadOnlySpan<ParticleEmitter> EmitterSpan => CollectionsMarshal.AsSpan(_emitters);


    internal void AttachRenderer(ParticleMeshGenerator meshGenerator)
    {
        _particleGenerator = meshGenerator;
    }

    public void SetMaterial(MaterialId materialId) => Material = materialId;


    public bool TryGetEmitter(string name, out ParticleEmitter emitter) => _byName.TryGetValue(name, out emitter!);

    public ParticleEmitter? GetEmitterOrNull(Handle<ParticleEmitter> handle)
    {
        var index = handle.Index();
        if ((uint)index >= _emitters.Count) return null;

        var emitter = _emitters[index];
        if (emitter != null && emitter.EmitterHandle.Value == handle.Value)
            return _emitters[index];

        var foundIndex = SortMethod.BinarySearchBy(CollectionsMarshal.AsSpan(_emitters), handle, out emitter);
        return foundIndex == -1 ? null : emitter;
    }

    public ParticleEmitter GetEmitter(Handle<ParticleEmitter> handle)
    {
        var index = handle.Index();
        if (index >= 0 && index < _emitters.Count && _emitters[index].EmitterHandle.Value == handle.Value)
            return _emitters[index];

        var foundIndex = SortMethod.BinarySearchBy(CollectionsMarshal.AsSpan(_emitters), handle, out var result);
        if (foundIndex < 0)
            throw new InvalidOperationException($"Missing emitter handle {handle}");


        return result!;
    }

    public ParticleEmitter CreateEmitter(string name, int particleCount, in ParticleDefinition definition)
    {
        if (_byName.ContainsKey(name)) throw new InvalidOperationException();

        var slot = _particleGenerator.CreateParticleMesh(particleCount, out var mesh);
        var handle = new Handle<ParticleEmitter>(slot + 1, 1);
        var emitter = new ParticleEmitter(name, handle, particleCount, in definition)
        {
            Mesh = mesh, Material = Material
        };

        if (_emitters.Count > 0 && GetEmitterOrNull(handle) != null)
            throw new InvalidOperationException();

        _emitters.Add(emitter);
        _byName[name] = emitter;

        _handleHigh = int.Max(_handleHigh, handle);

        emitter.Model = _meshTable.CreateSimpleModel(emitter.Mesh, 0, 4, ParticleComponent.DefaultParticleBounds);
        emitter.MaterialKey = _materialTable.Add(MaterialTagBuilder.BuildOne(emitter.Material));

        return emitter;
    }

    internal ParticleMeshWriter GetMeshWriterFor(ParticleEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter);
        return _particleGenerator.GetWriteBuffer(emitter);
    }

    internal void UpdateSimulate(float fixedDt)
    {
        SimulateEmitters(CollectionsMarshal.AsSpan(_emitters), fixedDt);

        var core = Ecs.Render.Core;
        foreach (var query in Ecs.Render.Query<ParticleComponent>())
        {
            var emitter = GetEmitter(query.Component.Emitter);
            emitter.State.Translation = core.GetTransform(query.RenderEntity).Transform.Translation;
        }
    }

/*
    private void UpdateEntities(WorldEntities entities)
    {
        BoundingBox currBox = default;
        int prevHandle = -1;

        var core = entities.Core;
        foreach (var query in entities.Query<ParticleComponent>())
        {
            var handle = query.Component.EmitterHandle;
            ref var box = ref core.GetBox(query.Entity);
            if (prevHandle != handle)
            {
                var emitter = GetEmitter(query.Component.EmitterHandle);
                emitter.UpdateLocalBounds(out currBox);
                prevHandle = emitter.EmitterHandle;
            }

            box.Bounds = currBox;
        }
    }
    */

    private static void SimulateEmitters(ReadOnlySpan<ParticleEmitter> emitters, float fixedDt)
    {
        foreach (var emitter in emitters)
        {
            if (emitter.State.Seed == 0) emitter.NewSeed();
            ref var state = ref emitter.State;
            ref readonly var def = ref emitter.Definition;

            var gravityStep = def.Gravity * fixedDt;
            var speedMinMax = def.SpeedMinMax;
            var lifeMinMax = def.LifeMinMax;

            var particles = emitter.ParticlesSpan;
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
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RespawnParticle(ref ParticleStateData p, ref ParticleEmitterState state, Vector2 speedMinMax,
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