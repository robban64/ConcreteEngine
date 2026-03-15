using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds.Mesh;

public abstract class ParticleEmitterBase : IComparable<ParticleEmitterBase>, IComparable<int>
{
    public readonly int EmitterHandle;
    public readonly MeshId Mesh;

    public string EmitterName { get; set; }
    public int ParticleCount { get; set; }

    public int CompareTo(ParticleEmitterBase? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : EmitterHandle.CompareTo(other.EmitterHandle);
    }

    public int CompareTo(int other) => EmitterHandle.CompareTo(other);
}

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<int>
{
    internal ParticleStateData[] Particles = [];

    public readonly int EmitterHandle;
    public string EmitterName { get; }
    public int ParticleCount { get; private set; }

    public readonly MeshId Mesh;

    internal ParticleState State;
    internal ParticleDefinition Definition;
    internal BoundingBox LocalBounds;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleDefinition GetDefinition() => ref Definition;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleState GetState() => ref State;

    internal Span<ParticleStateData> GetParticleData() => Particles.AsSpan(0, ParticleCount);

    public ParticleEmitter(string name, int handle, MeshId mesh, int particleCount,
        in ParticleDefinition def, in ParticleState state)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, 16);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(handle);

        EmitterName = name;
        EmitterHandle = handle;
        ParticleCount = particleCount;
        Definition = def;
        State = state;
        Mesh = mesh;
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
        if (State.Seed == 0) State.Seed = (uint)Environment.TickCount + (uint)EmitterHandle;
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

    public int CompareTo(int other) => EmitterHandle.CompareTo(other);
}