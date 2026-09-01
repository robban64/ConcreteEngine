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

    [InspectInclude]
    public readonly ParticleEmitterState State;

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

    public  Vector2 SpeedMinMax =>  State.SpeedMinMax;
    public  Vector2 LifeMinMax =>  State.LifeMinMax;


    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        if (BoundSlot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        BoundSlot = slot;
        BoundMesh = meshId;
        _data.UpdateLutFromParticleParams(State.StartColor,State.EndColor,State.SizeStartEnd);
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
    internal void Simulate(float simDt)
    {
        var dead = SimulateLife(simDt);
        if (dead > 0)
        {
            SimulateRespawn(_data.DeadIndices.Slice(0, dead));
        }

        SimulateSpatial(simDt);
    }


    [SkipLocalsInit]
    private unsafe void SimulateRespawn(NativeView<int> deadIndices)
    {
        var direction = new Vector4(State.Direction, 0);
        var spread = State.Spread;
        var rng = _rng;
        var particleState = _data.ParticleState.Ptr;
        var particleLife = _data.ParticleLifeState.Ptr;
        foreach (var index in deadIndices)
        {
            ref var p = ref particleState[index];
            var randDir = rng.NextVector3As4(-0.5f, 0.5f);
            p.Position = rng.NextVector3As4(-spread, spread);
            p.Velocity = Vector4.Normalize(randDir + direction) * rng.RandomFloat(SpeedMinMax);

            var life = rng.RandomFloat(LifeMinMax);
            particleLife[index] = new ParticleLifeState(life, 1f / life);
        }

        _rng = rng;
    }

    private unsafe int SimulateLife(float simDt)
    {
        int index = 0;
        var deadIndices = _data.DeadIndices.Ptr;
        foreach (var it in _data.ParticleLifeState.Zip(_data.ParticleInvLifeState))
        {
            float life = it.Item1.Life -= simDt;
            if (life > 0)
            {
                //
                it.Item2 = it.Item1.LutIndex(life);
                ++index;
            }
            else
            {
                *deadIndices++ = index;
                ++index;
            }
        }

        return (int)(deadIndices - _data.DeadIndices.Ptr);
    }

    [SkipLocalsInit]
    private void SimulateSpatial(float simDt)
    {
        var deltaVec4 = new Vector4(simDt);
        var gravityStep = new Vector4(State.Gravity * simDt, 0f);
        foreach (ref var p in _data.ParticleState)
        {
            var velocity = p.Velocity + gravityStep;
            var position = Vector4.FusedMultiplyAdd(velocity, deltaVec4, p.Position);
            p.Velocity = velocity;
            p.Position = position;
        }
    }

    private void InitializeParticles(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_data.Capacity)
            Throwers.RangeOutOfBounds(start, length, _data.Capacity);

        var rng = _rng;
        var lifeMinMax = State.LifeMinMax;
        var particleLifeState = _data.ParticleLifeState;
        for (var i = 0; i < length; i++)
        {
            var life = rng.RandomFloat(0, rng.RandomFloat(lifeMinMax));
            ref var p = ref particleLifeState[i];
            p.Life = life;
            p.LifeInvMax = 1f / life;
        }

        _rng = rng;
    }

    private void UpdateLocalBounds()
    {
        var max = Vector3.One * 5;
        _localBounds = new BoundingBox(-max, max);
    }

    public sealed class ParticleEmitterState(ParticleEmitter emitter, in EmitterParams emitterParams, in ParticleParams particleParams)
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
        public Vector2 SpeedMinMax { get; set => field = Set(field, value); }= emitterParams.SpeedMinMax;

        [InputNumber]
        [Segment("Simulation")]
        public Vector2 LifeMinMax { get; set => field = Set(field, value); }= emitterParams.LifeMinMax;

        private T Set<T>(T field, T value) where T : unmanaged
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return field;
            emitter._isDirty = true;
            return value;
        }
    }
}