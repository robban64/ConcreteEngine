using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<ushort>, IDisposable
{
    private const int MinCapacity = 128;
    public const int MinCount = 16;
    public const int MaxCount = 8192;

    private NativeArray<ParticleCpuInstance> _particles;

    public int ParticleCount { get; private set; }

    public int Slot { get; private set; } = -1;
    public readonly Id16<ParticleEmitter> Id;
    public MeshId BoundMesh;

    public readonly string Name;
    
    public Vector3 Translation;
    public Vector3 Direction;
    
    private FastRandom _rng;

    private EmitterVisualParams _visualsParams;
    private EmitterSpatialParams _spatialParams;
    private BoundingBox _localBounds;

    public ParticleEmitter(string name, Id16<ParticleEmitter> id, int particleCount,
        in EmitterSpatialParams def, in EmitterVisualParams visualParams)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        Name = name;
        Id = id;
        _spatialParams = def;
        _visualsParams = visualParams;
        ParticleCount = particleCount;
        _rng = new FastRandom((uint)Environment.TickCount + Id.Value);

        var length = int.Max(MinCapacity, IntMath.AlignUp(particleCount, 128));
        _particles = NativeArray.Allocate<ParticleCpuInstance>(length, zeroed: true);
        InitializeParticles(0, ParticleCount);
    }

    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Value);
        if(Slot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        BoundMesh = meshId;
    }

    public bool IsAttached => Slot >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox LocalBounds() => ref _localBounds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EmitterVisualParams VisualParams() => ref _visualsParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EmitterSpatialParams SpatialParams() => ref _spatialParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeView<ParticleCpuInstance> GetParticleView()
    {
        if (_particles.IsNull || _particles.Length < ParticleCount)
            Throwers.InvalidOperation("ParticleEmitter: invalid particle data");

        return _particles.Slice(0, ParticleCount);
    }


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

    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }

    public int CompareTo(ushort other) => Id.CompareTo(other);

    public void Dispose()
    {
        _particles.Dispose();
        Slot = -1;
        BoundMesh = default;
    }


    internal void UpdateLocalBounds(out BoundingBox bounds)
    {
        var distance = _spatialParams.LifeMinMax.Y * _spatialParams.LifeMinMax.Y;
        var extents = new Vector3(_spatialParams.Spread + distance);
        var min = -extents;
        var gravityOffset = 0.5f * _spatialParams.Gravity * (_spatialParams.LifeMinMax.Y * _spatialParams.LifeMinMax.Y);
        _localBounds.Min = Vector3.Min(min, min + gravityOffset);
        _localBounds.Max = Vector3.Max(extents, extents + gravityOffset);
        bounds = _localBounds;
    }

    private void InitializeParticles(int start, int length)
    {
        var particles = _particles.Slice(start, length);
        for (var i = 0; i < particles.Length; i++)
        {
            var randomMaxLife = _rng.RandomFloat(_spatialParams.LifeMinMax);
            ref var p = ref particles[i];
            p.MaxLife = randomMaxLife;
            p.Life = _rng.RandomFloat(0, randomMaxLife);
        }
    }

    
    internal void SimulateEmitter(float simDt)
    {
        var gravityStep = _spatialParams.Gravity * simDt;
        var particles = GetParticleView();
        var rng = _rng;
        for (int i = 0; i < particles.Length; i++)
        {
            ref var p = ref particles[i];
            if (p.Life <= 0)
            {
                rng = RespawnParticle(ref p, rng);
                continue;
            }

            p.Life -= simDt;
            p.Velocity += gravityStep;
            p.Position += p.Velocity * simDt;
        }

        _rng = rng;
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FastRandom RespawnParticle(ref ParticleCpuInstance p, FastRandom rng)
    {
        ref readonly var spatialParams = ref _spatialParams;
        var speed = rng.RandomFloat(spatialParams.SpeedMinMax);
        var spread = new Vector2(-spatialParams.Spread, spatialParams.Spread);
        var randDir = new Vector3(rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1)) * 0.5f;

        p.Position = Translation +
                     new Vector3(rng.RandomFloat(spread), rng.RandomFloat(spread), rng.RandomFloat(spread));

        p.Velocity = Vector3.Normalize(Direction + randDir) * speed;
        p.MaxLife = rng.RandomFloat(spatialParams.LifeMinMax);
        p.Life = p.MaxLife;
        return rng;
    }
}