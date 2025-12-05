#region

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.MeshGeneration;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class ParticleEmitter : IComparable<int>, IComparable<ParticleEmitter>
{
    public int EmitterHandle { get; }

    public int ParticleCount { get; set; }

    public MeshId MeshId { get; set; }
    public MaterialId MaterialId { get; set; }

    public ParticleEmitterState State;
    public ParticleDefinition Definition;

    internal ParticleStateData[] Particles;
    
    internal ReadOnlySpan<ParticleStateData> ParticlesSpan => Particles.AsSpan(0, ParticleCount);

    public ParticleEmitter(int handle, int particleCount, in ParticleDefinition def)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, 16);
        EmitterHandle = handle;
        ParticleCount = particleCount;
        Definition = def;
        Particles = new ParticleStateData[particleCount];
        
        var rng = new FastRandom((uint)DateTime.Now.Ticks); // Seed doesn't matter much here

        for (int i = 0; i < Particles.Length; i++)
        {
            ref var p = ref Particles[i];
            float randomMaxLife = rng.RandomFloat(Definition.LifeMinMax.X, Definition.LifeMinMax.Y);
            p.MaxLife = randomMaxLife;
            p.Life = rng.RandomFloat(0, randomMaxLife);
        }
    }

    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : EmitterHandle.CompareTo(other.EmitterHandle);
    }

    public int CompareTo(int other) => EmitterHandle.CompareTo(other);
}

public sealed class WorldParticles
{
    // public ModelId Model { get; private set; }
    public MaterialId Material { get; private set; }

    public float ParticleAlpha { get; private set; }
    public float ParticleDelta { get; private set; }
    private ParticleMeshGenerator _particleGenerator;
    private MaterialTable _materialTable;

    private readonly List<ParticleEmitter> _emitters = new(4);
    private int _handleHigh = 0;

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

    public ParticleEmitter CreateEmitter(int particleCount, ParticleDefinition definition)
    {
        var slotHandle = _particleGenerator.CreateParticleMesh(particleCount, out var mesh);
        var emitter = new ParticleEmitter(slotHandle, particleCount, in definition)
        {
            MeshId = mesh,
            MaterialId = Material
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

    internal void SimulateEmitters(float fixedDt, float alpha)
    {
        ParticleAlpha = alpha;
        ParticleDelta = fixedDt;
        _timer.Begin();
        foreach (var emitter in _emitters)
        {
            Simulate(emitter, fixedDt);
        }

        _timer.EndPrint();
    }


    private void Simulate(ParticleEmitter emitter, float fixedDt)
    {
        var particles = emitter.Particles; 
        ref readonly var state = ref emitter.State;      
        ref readonly var def = ref emitter.Definition;   

        var gravityStep = def.Gravity * fixedDt;
        var seed = (uint)((uint)Environment.TickCount * 1000) + (uint)emitter.EmitterHandle;
        var rng = new FastRandom(seed); 

        int len = particles.Length;
        for (int i = 0; i < len; i++)
        {
            ref var p = ref particles[i]; 
            if (p.Life <= 0)
            {
                RespawnParticle(ref p, ref rng, in state, in def);
                continue;
            }
            ProcessParticle(ref p, gravityStep, fixedDt);

        }


        return;

        static void ProcessParticle(ref ParticleStateData p, Vector3 gravityStep, float fixedDt)
        {
            p.Life -= fixedDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * fixedDt;

        }

        static void RespawnParticle(ref ParticleStateData p, ref FastRandom rng, in ParticleEmitterState state,
            in ParticleDefinition def)
        {
            var rx = rng.RandomFloat(-state.Spread, state.Spread);
            var ry = rng.RandomFloat(-state.Spread, state.Spread);
            var rz = rng.RandomFloat(-state.Spread, state.Spread);

            p.Position = state.Translation + new Vector3(rx, ry, rz);

            var randDir = new Vector3(rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1));

            var finalDir = (state.Direction + (randDir * 0.5f));
            if (finalDir != Vector3.Zero) finalDir = Vector3.Normalize(finalDir);

            var speed = rng.RandomFloat(def.SpeedMinMax.X, def.SpeedMinMax.Y);
            p.Velocity = finalDir * speed;

            p.MaxLife = rng.RandomFloat(def.LifeMinMax.X, def.LifeMinMax.Y);
            p.Life = p.MaxLife;

        }
    }
}