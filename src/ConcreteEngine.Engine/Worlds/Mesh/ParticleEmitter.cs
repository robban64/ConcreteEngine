using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<IntHandle<ParticleEmitter>>
{
    internal ParticleStateData[] Particles = [];

    public readonly IntHandle<ParticleEmitter> EmitterHandle;

    public string EmitterName;

    public int ParticleCount;

    public MeshId Mesh;
    public MaterialId Material;

    public ModelId Model;
    public MaterialTagKey MaterialKey;

    public ParticleEmitterState State;
    public ParticleDefinition Definition;
    public BoundingBox LocalBounds;

    public Vector3 OriginTranslation
    {
        get => field;
        set
        {
            field = value;
            State.Translation = field;
        }
    }

    internal TuplePtr<ParticleEmitterState, ParticleDefinition> GetStateDefPtr() => new(ref State, ref Definition);
    internal Span<ParticleStateData> GetParticleData() => Particles.AsSpan(0, ParticleCount);

    public ParticleEmitter(string name, IntHandle<ParticleEmitter> handle, int particleCount, in ParticleDefinition def)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, 16);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(handle.Value);

        EmitterName = name;
        EmitterHandle = handle;
        ParticleCount = particleCount;
        Definition = def;
        Particles = new ParticleStateData[particleCount];

        NewSeed();
        var rng = new FastRandom(State.Seed);

        for (int i = 0; i < Particles.Length; i++)
        {
            ref var p = ref Particles[i];
            var randomMaxLife = rng.RandomFloat(Definition.LifeMinMax.X, Definition.LifeMinMax.Y);
            p.MaxLife = randomMaxLife;
            p.Life = rng.RandomFloat(0, randomMaxLife);
        }
    }

    internal void NewSeed()
    {
        if (State.Seed == 0) State.Seed = (uint)Environment.TickCount + (uint)EmitterHandle.Value;
    }

    internal void UpdateLocalBounds(out BoundingBox bounds)
    {
        var distance = Definition.LifeMinMax.Y * Definition.LifeMinMax.Y;
        var extents = new Vector3(State.Spread + distance);
        var min = -extents;
        var gravityOffset = 0.5f * Definition.Gravity * (Definition.LifeMinMax.Y * Definition.LifeMinMax.Y);
        LocalBounds.Min = Vector3.Min(min, min + gravityOffset);
        LocalBounds.Max = Vector3.Max(extents, extents + gravityOffset);
        bounds = LocalBounds;
    }

    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : EmitterHandle.CompareTo(other.EmitterHandle);
    }

    public int CompareTo(IntHandle<ParticleEmitter> other) => EmitterHandle.CompareTo(other);
}