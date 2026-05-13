using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Graphics.Handles;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<int>, IDisposable
{
    private const int MinCapacity = 128;
    public const int MinCount = 16;
    public const int MaxCount = 8192;

    private NativeArray<ParticleStateData> _particles;

    public int ParticleCount { get; private set; }

    public readonly Id32<ParticleEmitter> Id;
    public readonly int Slot;
    
    public readonly string Name;

    public Vector3 Translation;
    public Vector3 Direction;
    public uint Seed;

    private EmitterVisualParams _visuals;
    private EmitterSpatialParams _spatial;
    private BoundingBox _localBounds;


    public ParticleEmitter(string name, Id32<ParticleEmitter> id, int slot, int particleCount,
        in EmitterSpatialParams def, in ParticleState state)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value);
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        Name = name;
        Id = id;
        Slot = slot;
        _spatial = def;
        Translation = state.Translation;
        Direction = state.Direction;
        Seed = state.Seed;
        
        ParticleCount = particleCount;

        var length = int.Max(MinCapacity, IntMath.AlignUp(particleCount, 128));
        _particles = NativeArray.Allocate<ParticleStateData>(length, zeroed: true);
        InitializeParticles(0, ParticleCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EmitterVisualParams LocalBounds() => ref _visuals;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EmitterVisualParams VisualParams() => ref _visuals;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EmitterSpatialParams SpatialParams() => ref _spatial;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<ParticleStateData> GetParticleSpan()
    {
        if(_particles.IsNull || _particles.Length < ParticleCount)
            Throwers.InvalidOperation("ParticleEmitter: invalid particle data");
        
        return _particles.AsSpan(0, ParticleCount);
    }

    // TODO event?
    public void SetCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);

        if (count == ParticleCount) return;

        var newCapacity = int.Max(MinCapacity, IntMath.AlignUp(count, 128));
        if (newCapacity > _particles.Length)
        {
            _particles.Resize(newCapacity, true);
            Logger.LogString(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
            ParticleCount = count;
            return;
        }

        var previousCount = ParticleCount;
        ParticleCount = count;
        InitializeParticles(previousCount, ParticleCount - previousCount);
    }


    internal void NewSeed()
    {
        if (Seed == 0) Seed = (uint)Environment.TickCount + (uint)Id.Value;
    }

    internal void UpdateLocalBounds(out BoundingBox bounds)
    {
        var distance = _spatial.LifeMinMax.Y * _spatial.LifeMinMax.Y;
        var extents = new Vector3(_spatial.Spread + distance);
        var min = -extents;
        var gravityOffset = 0.5f * _spatial.Gravity * (_spatial.LifeMinMax.Y * _spatial.LifeMinMax.Y);
        _localBounds.Min = Vector3.Min(min, min + gravityOffset);
        _localBounds.Max = Vector3.Max(extents, extents + gravityOffset);
        bounds = _localBounds;
    }

    private void InitializeParticles(int start, int length)
    {
        NewSeed();
        var rng = new FastRandom(Seed);
        foreach (ref var p in _particles.Slice(start, length))
        {
            var randomMaxLife = rng.RandomFloat(_spatial.LifeMinMax.X, _spatial.LifeMinMax.Y);
            p.MaxLife = randomMaxLife;
            p.Life = rng.RandomFloat(0, randomMaxLife);
        }
    }

    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }

    public int CompareTo(int other) => Id.CompareTo(other);

    public void Dispose() => _particles.Dispose();
}