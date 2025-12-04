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
    //public const int MinBufferSize = 64;
    public int EmitterHandle { get; }

    public int ParticleCount { get; set; }

    public MeshId MeshId { get; set; }
    public MaterialId MaterialId { get; set; }

    public ParticleEmitterState State;
    public ParticleDefinition Definition;

    internal ParticleStateData[] Particles;

    public ParticleEmitter(int handle, int particleCount, in ParticleDefinition def)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, 16);
        EmitterHandle = handle;
        ParticleCount = particleCount;
        Definition = def;
        Particles = new ParticleStateData[particleCount];
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


    private FrameProfileTimer _timer = new();

    internal void SimulateEmitters(float fixedDt, float totalTime, Vector3 cameraPos)
    {
        _timer.Begin();
        foreach (var emitter in _emitters)
        {
            Simulate(emitter, fixedDt, totalTime, cameraPos);
        }

        if (_timer.End())
        {
            Console.WriteLine("Simulate: " + _timer.ResultString);
        }
    }

    private float _translationTicker = 1;
    private Vector3 _lastSampleTranslation = default;

    private void Simulate(ParticleEmitter emitter, float fixedDt, float totalTime, Vector3 cameraPos)
    {
        const float spread = 0.2f;

        _translationTicker += fixedDt;
        if (_translationTicker >= 1)
        {
            _lastSampleTranslation = cameraPos;
            _translationTicker = 0;
        }

        if (_lastSampleTranslation == default && cameraPos == default) return;

        ref var def = ref emitter.Definition;
        ref var state = ref emitter.State;
        state.Translation = Vector3.Lerp(_lastSampleTranslation, cameraPos, float.Min(_translationTicker, 1f));


        var particles = emitter.Particles;
        var startArea = emitter.State.StartArea;
        var direction = emitter.State.Direction;
        var startPos = emitter.State.Translation;

        var gravityStep = def.Gravity * fixedDt;

        var len = particles.Length;
        for (var i = 0; i < len; i++)
        {
            ref var particle = ref particles[i];
            if (particle.Life < 0)
            {
                ProcessDeadParticle(ref particle, startArea, direction, startPos, in def);
                continue;
            }

            ProcessParticle(ref particle, gravityStep, totalTime, fixedDt);
        }

        return;

        static void ProcessParticle(ref ParticleStateData particle, Vector3 gravityStep, float totalTime, float fixedDt)
        {
            var waveX = MathF.Sin(totalTime * 0.5f + particle.OriginalSpawnPos.Y);
            var waveZ = MathF.Cos(totalTime * 0.3f + particle.OriginalSpawnPos.X);
            var turbulence = new Vector3(waveX, 0, waveZ) * 0.1f;

            particle.PrevPosition = particle.Position;
            particle.Velocity += gravityStep;
            particle.Position += (particle.Velocity + turbulence) * fixedDt;
            particle.Life -= fixedDt;
        }

        static void ProcessDeadParticle(ref ParticleStateData particle, Vector3 startArea, Vector3 direction,
            Vector3 startPos, in ParticleDefinition def)
        {
            var rng = new FastRandom((uint)Environment.TickCount);

            var offset = new Vector3(
                rng.RandomFloat(-startArea.X, startArea.X),
                rng.RandomFloat(-startArea.Y, startArea.Y),
                rng.RandomFloat(-startArea.Z, startArea.Z));

            particle.Position = startPos + offset;
            particle.PrevPosition = particle.Position;
            particle.OriginalSpawnPos = particle.Position;
            particle.MaxLife = rng.RandomFloat(def.LifeMinMax);
            particle.Life = particle.MaxLife;

            var rngDir = new Vector3(rng.RandomFloat(-1f, 1f), rng.RandomFloat(-1f, 1f), rng.RandomFloat(-1f, 1f));
            rngDir = Vector3.Normalize(rngDir);
            particle.Velocity = Vector3.Normalize(direction + (rngDir * spread));
        }
    }
}