using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<int>
{
    public const int MinCount = 16;
    public const int MaxCount = 8192;

    public readonly MeshId MeshId;
    public readonly int EmitterHandle;

    internal ParticleState State;
    internal ParticleDefinition Definition;

    private ParticleStateData[] _particles;

    public string EmitterName { get; }
    public int ParticleCount { get; private set; }
    internal int PreviousCount { get; set; }

    internal BoundingBox LocalBounds;

    public ParticleEmitter(string name, int handle, MeshId meshId, int particleCount,
        in ParticleDefinition def, in ParticleState state)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(handle);

        EmitterName = name;
        EmitterHandle = handle;
        Definition = def;
        State = state;
        MeshId = meshId;

        ParticleCount = PreviousCount = particleCount;
        _particles = new ParticleStateData[IntMath.AlignUp(particleCount, 64)];
        InitializeParticles(0, ParticleCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleDefinition GetDefinition() => ref Definition;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleState GetState() => ref State;

    internal Span<ParticleStateData> GetParticleData() => _particles.AsSpan(0, ParticleCount);

    // TODO event?
    public void SetCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);

        var newCapacity = IntMath.AlignUp(count, 64);
        if (newCapacity == ParticleCount) return;

        if (newCapacity < ParticleCount)
        {
            Array.Clear(_particles, newCapacity, ParticleCount - newCapacity);
            ParticleCount = newCapacity;
            return;
        }

        Array.Resize(ref _particles, newCapacity);
        var previousCount = ParticleCount;
        ParticleCount = newCapacity;
        InitializeParticles(previousCount, ParticleCount - previousCount);
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

    private void InitializeParticles(int start, int length)
    {
        NewSeed();
        var rng = new FastRandom(State.Seed);
        foreach (ref var p in _particles.AsSpan(start, length))
        {
            var randomMaxLife = rng.RandomFloat(Definition.LifeMinMax.X, Definition.LifeMinMax.Y);
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