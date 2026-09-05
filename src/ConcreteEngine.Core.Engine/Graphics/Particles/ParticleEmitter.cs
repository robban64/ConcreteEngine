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

    private bool _isDirty;
    private FastRandom _rng;

    private readonly ParticleEmitterData _data;

    public readonly Id16<ParticleEmitter> Id;

    public readonly string Name;

    [InspectInclude] public readonly ParticleEmitterState State;

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
        State = new ParticleEmitterState(this, in emitterParams, in particleParams);
        ParticleCount = PendingParticleCount = particleCount;
        _rng = new FastRandom((uint)Environment.TickCount + Id.Id);

        var length = int.Max(ParticleEmitterData.MinCapacity, IntMath.AlignUp(particleCount, 128));
        _data = new ParticleEmitterData(length);
        InitializeParticles(0, ParticleCount);
    }

    public bool IsDirty => _isDirty;
    public bool IsAttached => BoundSlot >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox LocalBounds() => ref _localBounds;

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
    internal ParticleEmitterData GetEmitterData()
    {
        if (_data.IsNullOrEmpty) Throwers.InvalidOperation("ParticleEmitter: null or empty emitter data");
        return _data;
    }

    // TODO
    public void SetCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);

        if (count == ParticleCount || count == PendingParticleCount) return;
        // PendingParticleCount = count;
        // _isDirty = true;
    }

    internal void Commit()
    {
        _isDirty = false;

        UpdateLocalBounds();

        if (PendingParticleCount == ParticleCount) return;

        var alignedCapacity = IntMath.AlignUp(PendingParticleCount, 128);
        var newCapacity = int.Max(ParticleEmitterData.MinCapacity, alignedCapacity);
        if (newCapacity > _data.Capacity)
            _data.ReAlloc(newCapacity);

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
        _data.Dispose();
        BoundSlot = -1;
        BoundMesh = default;
    }

    [SkipLocalsInit]
    internal void RespawnParticles(ReadOnlySpan<int> deadIndices)
    {
        var rng = _rng;
        var data = _data;

        var direction = new Vector4(State.Direction, 0);
        var speedMinMax = State.SpeedMinMax;
        foreach (var index in deadIndices)
        {
            var speed = rng.RandomFloat(speedMinMax);
            var randDir = rng.NextVector3As4(-0.5f, 0.5f);
            var velocity = Vector4.Normalize(randDir + direction) * speed;
            data.SetVelocity(index, velocity);
        }

        var spread = State.Spread;
        foreach (var index in deadIndices)
            data.SetPosition(index, rng.NextVector3(-spread, spread));


        var lifeMinMax = State.LifeMinMax;
        foreach (var index in deadIndices)
            data.SetLife(index, rng.RandomFloat(lifeMinMax));

        _rng = rng;
    }

    private void InitializeParticles(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_data.Capacity)
            Throwers.RangeOutOfBounds(start, length, _data.Capacity);

        var rng = _rng;
        var lifeMinMax = State.LifeMinMax;
        var particleLifeState = _data.LifeStates(ParticleCount);
        for (var i = start; i < length; i++)
        {
            var life = rng.RandomFloat(0, rng.RandomFloat(lifeMinMax));
            particleLifeState[i] = new ParticleLifeState(1, 1f / life);
        }

        _rng = rng;
    }

    private void UpdateLocalBounds()
    {
        var max = Vector3.One * 5;
        _localBounds = new BoundingBox(-max, max);
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