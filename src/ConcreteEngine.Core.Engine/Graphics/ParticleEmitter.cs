using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<int>
{
    public const int MinCount = 16;
    public const int MaxCount = 8192;
    
    private ParticleStateData[] Particles = [];

    public readonly int EmitterHandle;
    public string EmitterName { get; }
    public int ParticleCount { get; private set; }

    public readonly MeshId Mesh;

    internal ParticleState State;
    internal ParticleDefinition Definition;
    internal BoundingBox LocalBounds;



    public ParticleEmitter(string name, int handle, MeshId mesh, int particleCount,
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
        Mesh = mesh;
        
        ParticleCount = particleCount;
        Particles = new ParticleStateData[IntMath.AlignUp(particleCount,64)];

        NewSeed();
        var rng = new FastRandom(State.Seed);

        for (int i = 0; i < ParticleCount; i++)
        {
            ref var p = ref Particles[i];
            var randomMaxLife = rng.RandomFloat(Definition.LifeMinMax.X, Definition.LifeMinMax.Y);
            p.MaxLife = randomMaxLife;
            p.Life = rng.RandomFloat(0, randomMaxLife);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleDefinition GetDefinition() => ref Definition;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ParticleState GetState() => ref State;
    internal Span<ParticleStateData> GetParticleData() => Particles.AsSpan(0, ParticleCount);

    // TODO event?
    public void SetCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);

        var newCapacity = IntMath.AlignUp(count,64);
        if(newCapacity == ParticleCount) return;

        if (newCapacity < Particles.Length)
        {
            Array.Clear(Particles, count, ParticleCount - count);
            ParticleCount = count;
            return;
        }
        
        Array.Resize(ref Particles, newCapacity);
        ParticleCount = count;
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