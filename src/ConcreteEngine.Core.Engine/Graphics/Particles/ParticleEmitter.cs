using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[Inspect]
public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<ushort>, IDisposable
{
    public const int MinCount = 16;
    public const int MaxCount = 8192;
    public const int CountAlignment = 16;

    private bool _isDirty;
    private FastRandom _rng;
    
    public readonly Id16<ParticleEmitter> Id;

    public readonly string Name;

    [InspectInclude] public readonly ParticleEmitterState State;
    
    private readonly ParticleEmitterData _data;

    public MeshId BoundMesh { get; private set; }
    public int BoundSlot { get; private set; } = -1;
    public int ParticleCount { get; private set; }
    public int PendingParticleCount { get; private set; }

    private BoundingBox _localBounds;

    public ParticleEmitter(string name, Id16<ParticleEmitter> id, int particleCount,
        in EmitterParams emitterParams, in ParticleParams particleParams)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id.Id);
        ArgumentOutOfRangeException.ThrowIfLessThan(particleCount, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(particleCount, MaxCount);

        Name = name;
        Id = id;
        ParticleCount = PendingParticleCount = particleCount;
        State = new ParticleEmitterState(this, in emitterParams, in particleParams);
        _rng = new FastRandom((uint)Environment.TickCount + Id.Id);

        var length = int.Max(ParticleEmitterData.MinCapacity, IntMath.AlignUp(particleCount, CountAlignment));
        _data = new ParticleEmitterData(length);
        InitializeParticles(0, ParticleCount);
    }

    public int AlignedParticleCount => IntMath.AlignUp(ParticleCount, 16);

    public bool IsDirty => _isDirty;
    public bool IsAttached => BoundSlot >= 0;

    public ref readonly BoundingBox LocalBounds => ref _localBounds;

    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        if (BoundSlot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        BoundSlot = slot;
        BoundMesh = meshId;
        _data.UpdateLutFromParticleParams(State.StartColor, State.EndColor, State.SizeStartEnd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ParticleEmitterData GetData()
    {
        if (_data.IsNullOrEmpty) Throwers.NullPointer("ParticleEmitter: null or empty emitter data");
        return _data;
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

        if (PendingParticleCount != ParticleCount)
        {
            int prevCount = ParticleCount, newCount = PendingParticleCount;
            var alignedCount = IntMath.AlignUp(newCount, CountAlignment);
            _data.EnsureAllocate(alignedCount);

            ParticleCount = newCount;
            PendingParticleCount = 0;
            
            if (newCount > prevCount)
                InitializeParticles(prevCount, newCount - prevCount);
        }
    }
    
    
    private void InitializeParticles(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start + (uint)length, (uint)ParticleCount);

        var rng = _rng;
        var lifeMinMax = State.LifeMinMax;
        
        var lifeState = _data.LifeState.AsSpan(start, length);
        var lifeMaxInverse = _data.LifeMaxInverse.AsSpan(start, length);
        
        for (var i = 0; i < lifeState.Length; i++)
        {
            var life = rng.RandomFloat(lifeMinMax);
            lifeState[i] = life;
            lifeMaxInverse[i] = 1f / life;
        }

        _rng = rng;
    }

    [SkipLocalsInit]
    internal void RespawnParticles(ReadOnlySpan<ushort> deadIndices)
    {
        var rng = _rng;

        var direction = State.Direction.AsVector128();
        var speedMinMax = State.SpeedMinMax;
        var velocities = _data.Velocities;
        foreach (var index in deadIndices)
        {
            var speed = rng.RandomFloat(speedMinMax);
            var randDir = rng.NextVector3(-0.5f, 0.5f).AsVector128();
            var velocity = VectorMath.Normalize(randDir + direction) * speed;
            ref var dst = ref velocities[index];
            velocity.StoreUnsafe(ref Unsafe.As<Vector4, float>(ref dst));
        }

        var spread = State.Spread;
        var positions = _data.Positions;
        foreach (var index in deadIndices)
        {
            var pos = rng.NextVector3(-spread, spread);
            ref var dst = ref positions[index];
            Unsafe.As<Vector4, Vector3>(ref dst) = pos;
        }


        var lifeMinMax = State.LifeMinMax;
        var lifeState = _data.LifeState;
        var lifeMaxInverse = _data.LifeMaxInverse;
        foreach (var index in deadIndices)
        {
            var life = rng.RandomFloat(lifeMinMax);
            lifeState[index] = life;
            lifeMaxInverse[index] = 1f / life;
        }

        _rng = rng;
    }

    private void UpdateLocalBounds()
    {
        var max = Vector3.One * 5;
        _localBounds = new BoundingBox(-max, max);
    }

    public int CompareTo(ParticleEmitter? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }

    public int CompareTo(ushort other) => Id.CompareTo(other);

    public void Dispose()
    {
        _data.Dispose();
        BoundSlot = -1;
        BoundMesh = default;
    }

    public sealed class ParticleEmitterState(
        ParticleEmitter emitter,
        in EmitterParams emitterParams,
        in ParticleParams particleParams)
    {
        [InputColor]
        [Segment("Visual")]
        public ColorRgba StartColor { get; set => field = Set(field, value); } = particleParams.StartColor;

        [InputColor]
        [Segment("Visual")]

        public ColorRgba EndColor { get; set => field = Set(field, value); } = particleParams.EndColor;

        [InputNumber]
        [Segment("Visual")]
        public Vector2 SizeStartEnd { get; set => field = Set(field, value); } = particleParams.SizeStartEnd;

        [InputNumber]
        [Segment("Simulation")]
        public float Spread { get; set => field = Set(field, value); } = emitterParams.Spread;

        [InputNumber]
        [Segment("Simulation")]
        public Vector3 Gravity { get; set => field = Set(field, value); } = new(0.0f, 0.015f, 0.0f);

        [InputNumber]
        [Segment("Simulation")]
        public Vector3 Direction { get; set => field = Set(field, value); } = emitterParams.Direction;

        [InputNumber]
        [Segment("Simulation")]
        public Vector2 SpeedMinMax { get; set => field = Set(field, value); } = emitterParams.SpeedMinMax;

        [InputNumber]
        [Segment("Simulation")]
        public Vector2 LifeMinMax { get; set => field = Set(field, value); } = emitterParams.LifeMinMax;

        private T Set<T>(T field, T value) where T : unmanaged
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return field;
            emitter._isDirty = true;
            return value;
        }
    }
}