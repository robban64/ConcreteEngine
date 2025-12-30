using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.World;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<Handle<ParticleEmitter>>
{
    public readonly Handle<ParticleEmitter> EmitterHandle;

    public int ParticleCount;
    public MeshId Mesh;
    public MaterialId Material;

    public ModelId Model;
    public MaterialTagKey MaterialKey;

    public ParticleEmitterState State;
    public ParticleDefinition Definition;
    public BoundingBox LocalBounds;

    internal ParticleStateData[] Particles = [];

    public string EmitterName;


    internal TuplePtr<ParticleEmitterState, ParticleDefinition> GetStateDefPtr() => new(ref State, ref Definition);
    internal Span<ParticleStateData> ParticlesSpan => Particles.AsSpan(0, ParticleCount);

    public ParticleEmitter(string name, Handle<ParticleEmitter> handle, int particleCount, in ParticleDefinition def)
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

    public int CompareTo(Handle<ParticleEmitter> other) => EmitterHandle.CompareTo(other);
}