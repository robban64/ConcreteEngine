using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[Inspect]
public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<ushort>, IDisposable
{
    private const int MinCapacity = 128;
    public const int MinCount = 16;
    public const int MaxCount = 8192;

    private bool _isDirty;
    private FastRandom _rng;
    private NativeArray<ParticleState> _particles;

    public readonly Id16<ParticleEmitter> Id;

    public readonly string Name;

    public MeshId BoundMesh { get; private set; }

    public int Slot { get; private set; } = -1;

    public int ParticleCount { get; private set; }
    public int PendingParticleCount { get; private set; }


    private ParticleParams _particleParams;
    private EmitterParams _emitterParams;
    [InputNumber(Segment = "Simulation")] public Vector3 Gravity = new Vector3(0.0f, 0.015f, 0.0f);
    [InputNumber(Segment = "Simulation")] public float Drag;

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
        _emitterParams = emitterParams;
        _particleParams = particleParams;
        ParticleCount = PendingParticleCount = particleCount;
        _rng = new FastRandom((uint)Environment.TickCount + Id.Value);

        var length = int.Max(MinCapacity, IntMath.AlignUp(particleCount, 128));
        _particles = NativeArray.Allocate<ParticleState>(length, zeroed: true);
        InitializeParticles(0, ParticleCount);
    }

    public bool IsDirty => _isDirty;
    public bool IsAttached => Slot >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly EmitterParams GetEmitterParams() => ref _emitterParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ParticleParams GetParticleParams() => ref _particleParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox LocalBounds() => ref _localBounds;

    //TEMP
    [InspectInclude] public ref EmitterParams EmitterParams => ref _emitterParams;
    [InspectInclude] public ref ParticleParams ParticleParams => ref _particleParams;
    //

    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        if (Slot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        BoundMesh = meshId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeView<ParticleState> GetParticleView()
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
            Logger.Log(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
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

        var gravityStep = Gravity * simDt;
        var spawnParam = _emitterParams;

        var rng = _rng;
        foreach (ref var p in _particles.Slice(0, ParticleCount))
        {
            var life = p.Life;
            if (life <= 0f)
            {
                var speed = rng.RandomFloat(spawnParam.SpeedMinMax);
                var randDir = rng.NextVector3(-0.5f, 0.5f);
                p.Position = rng.NextVector3(-spawnParam.Spread, spawnParam.Spread);
                p.Velocity = Vector3.Normalize(spawnParam.Direction + randDir) * speed;

                life = rng.RandomFloat(spawnParam.LifeMinMax);
                p.Life = life;
                p.InvMaxLife = 1f / life;
                p.InvLife = 0f;
                continue;
            }

            var velocity = p.Velocity + gravityStep;
            p.Velocity = velocity;
            p.Position += velocity * simDt;

            life -= simDt;
            p.Life = life;
            p.InvLife = float.Clamp(1f - life * p.InvMaxLife, 0f, 1f);
        }

        _rng = rng;
    }

    private void InitializeParticles(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_particles.Length)
            Throwers.IndexOutOfRange(nameof(_particles), start + length, _particles.Length);

        var rng = _rng;
        var particles = _particles.Slice(start, length);
        for (var i = 0; i < particles.Length; i++)
        {
            ref var p = ref particles[i];
            var randomMaxLife = rng.RandomFloat(_emitterParams.LifeMinMax);
            p.Life = rng.RandomFloat(0, randomMaxLife);
            p.InvMaxLife = 1f / p.Life;
        }

        _rng = rng;
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