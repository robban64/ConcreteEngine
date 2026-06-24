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

    public readonly Id16<ParticleEmitter> Id;
    public readonly string Name;

    public int Slot { get; private set; } = -1;
    public MeshId BoundMesh { get; private set; }

    public int ParticleCount { get; private set; }
    public int PendingParticleCount { get; private set; }

    private bool _isDirty;

    private FastRandom _rng;

    private ParticleParams _particlesParams;
    private EmitterParams _params;
    private BoundingBox _localBounds;

    public ParticleEmitter(string name, Id16<ParticleEmitter> id, int particleCount,
        in EmitterParams emitterParams, in ParticleParams particleParams)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Value);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        Name = name;
        Id = id;
        _params = emitterParams;
        _particlesParams = particleParams;
        ParticleCount = PendingParticleCount = particleCount;
        _rng = new FastRandom((uint)Environment.TickCount + Id.Value);

        var length = int.Max(MinCapacity, IntMath.AlignUp(particleCount, 128));
        _particles = NativeArray.Allocate<ParticleCpuInstance>(length, zeroed: true);
        InitializeParticles(0, ParticleCount);
    }

    public bool IsDirty => _isDirty;
    public bool IsAttached => Slot >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox LocalBounds() => ref _localBounds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ParticleParams GetParticleParams() => ref _particlesParams;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly EmitterParams GetEmitterParams() => ref _params;

    public StateScope<ParticleParams> ParticleParams => new(ref _particlesParams, ref _isDirty);
    public StateScope<EmitterParams> EmitterParams => new(ref _params, ref _isDirty);

    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Value);
        if (Slot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        BoundMesh = meshId;
    }

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

        if (count == ParticleCount || count == PendingParticleCount) return;
        PendingParticleCount = count;
        _isDirty = true;
    }

    internal void Commit()
    {
        _isDirty = false;

        UpdateLocalBounds();

        if (PendingParticleCount == ParticleCount) return;

        var newCapacity = int.Max(MinCapacity, IntMath.AlignUp(PendingParticleCount, 128));
        if (newCapacity > _particles.Length)
        {
            _particles.Resize(newCapacity, true);
            Logger.LogString(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
        }

        if (PendingParticleCount > ParticleCount)
            InitializeParticles(ParticleCount, PendingParticleCount - ParticleCount);

        ParticleCount = PendingParticleCount;
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

    internal void Simulate(float simDt)
    {
        if (!IsAttached) return;

        var gravityStep = _params.Gravity * simDt;
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
        ref readonly var spatialParams = ref _params;
        var speed = rng.RandomFloat(spatialParams.SpeedMinMax);
        var randDir = new Vector3(rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1), rng.RandomFloat(-1, 1)) * 0.5f;
        var spread = new Vector2(-spatialParams.Spread, spatialParams.Spread);

        p.Position = new Vector3(rng.RandomFloat(spread), rng.RandomFloat(spread), rng.RandomFloat(spread));
        p.Velocity = Vector3.Normalize(spatialParams.Direction + randDir) * speed;
        p.MaxLife = rng.RandomFloat(spatialParams.LifeMinMax);
        p.Life = p.MaxLife;
        return rng;
    }

    private void InitializeParticles(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_particles.Length)
            Throwers.IndexOutOfRange(nameof(_particles), start + length, _particles.Length);

        var particles = _particles.Slice(start, length);
        for (var i = 0; i < particles.Length; i++)
        {
            var randomMaxLife = _rng.RandomFloat(_params.LifeMinMax);
            ref var p = ref particles[i];
            p.MaxLife = randomMaxLife;
            p.Life = _rng.RandomFloat(0, randomMaxLife);
        }
    }

    private void UpdateLocalBounds()
    {
        var max = Vector3.One * 5;
        _localBounds = new BoundingBox(-max, max);
        /*
        ref readonly var param = ref _spatialParams;
        var distance = param.LifeMinMax.Y * param.LifeMinMax.Y;
        var extents = new Vector3(param.Spread + distance);
        var min = -extents;
        var gravityOffset = 0.5f * param.Gravity * (param.LifeMinMax.Y * param.LifeMinMax.Y);
        _localBounds.Min = Vector3.Min(min, min + gravityOffset);
        _localBounds.Max = Vector3.Max(extents, extents + gravityOffset);
        */
    }
}