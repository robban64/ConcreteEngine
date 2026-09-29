using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        InitializeParticles(0, particleCount);
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

    //
    internal void Simulate(ushort[] deadIndices, float simDt)
    {
        if (_data.IsNullOrEmpty) Throwers.NullPointer("ParticleEmitter: null or empty emitter data");

        var count = AlignedParticleCount;
        var dead = SimulateLife(deadIndices, count, simDt);
        if (dead > 0)
        {
            RespawnParticles(deadIndices.AsSpan(0, dead));
        }

        SimulateLifeIndex(count);
        SimulateSpatial(count, simDt);
    }

    private int SimulateLife(ushort[] deadIndicesArray, int count, float simDt)
    {
        int deadIndex = 0;
        ref var deadIndices = ref MemoryMarshal.GetArrayDataReference(deadIndicesArray);

        var lifeSpan = _data.LifeState.AsSpan(0, count);
        for (int i = 0; i <= lifeSpan.Length - Vector256<float>.Count; i += Vector256<float>.Count)
        {
            ref var life = ref lifeSpan[i];
            var vLife = Vector256.Subtract(Vector256.LoadUnsafe(ref life), Vector256.Create(simDt));
            var mask = Vector256.LessThanOrEqual(vLife, Vector256<float>.Zero).ExtractMostSignificantBits();
            vLife.StoreUnsafe(ref life);

            while (mask != 0)
            {
                var p = BitOperations.TrailingZeroCount(mask);
                Unsafe.Add(ref deadIndices, deadIndex++) = (ushort)(i + p);
                mask &= mask - 1;
            }
        }

        return deadIndex;
    }

    private void SimulateLifeIndex(int count)
    {
        var lifeState = _data.LifeState.Slice(0, count).Reinterpret<Vector256<float>>();
        var invMaxLifeState = _data.LifeMaxInverse.Slice(0, count).Reinterpret<Vector256<float>>();
        var lutIndices = _data.LifeLutIndices.Slice(0, count).Reinterpret<long>(); // Note: long works for Vector256

        foreach (var it in lifeState.Zip(invMaxLifeState, lutIndices))
        {
            var vDiff = Fma.MultiplyAddNegated(it.Item1, it.Item2, Vector256<float>.One);
            var vIndex = Fma.MultiplyAdd(vDiff, Vector256.Create(255f), Vector256.Create(0.5f));

            var vIndexInt32 = Avx.ConvertToVector256Int32(vIndex);
            var bytes = Sse2.PackUnsignedSaturate(
                Sse2.PackSignedSaturate(vIndexInt32.GetLower(), vIndexInt32.GetUpper()), 
                Vector128<short>.Zero
            );

            it.Item3 = bytes.AsInt64().ToScalar();
        }
    }

    private void SimulateSpatial(int count, float simDt)
    {
        var velocities = _data.Velocities.Slice(0, count).Reinterpret<Vector256<float>>();
        var positions = _data.Positions.Slice(0, count).Reinterpret<Vector256<float>>();
        var gravityStep256 = Vector256.Create(State.Gravity.AsVector128() * simDt);
        foreach (var it in velocities.Zip(positions))
        {
            var vVelocity = Vector256.Add(it.Item1, gravityStep256);
            var vPosition = Fma.MultiplyAdd(vVelocity, Vector256.Create(simDt), it.Item2);
            it.Item1 = vVelocity;
            it.Item2 = vPosition;
        }
    }

    [SkipLocalsInit]
    private void RespawnParticles(ReadOnlySpan<ushort> deadIndices)
    {
        var rng = _rng;
        var data = _data;
        var spread = State.Spread;
        var lifeMinMax = State.LifeMinMax;
        var speedMinMax = State.SpeedMinMax;
        var direction = State.Direction;
        foreach (var index in deadIndices)
        {
            var life = rng.RandomFloat(lifeMinMax);
            var speed = rng.RandomFloat(speedMinMax);
            var position = rng.RandomVector3(-spread, spread);
            var randDir = rng.RandomVector3(-0.5f, 0.5f);
            var velocity = Vector3.Normalize(randDir + direction) * speed;
            data.Set(index, velocity, position, life);
        }

        _rng = rng;
    }

    private void InitializeParticles(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)start + (uint)length, (uint)ParticleCount);

        var rng = _rng;
        var spread = State.Spread;
        var lifeMinMax = State.LifeMinMax;
        var speedMinMax = State.SpeedMinMax;
        var direction = State.Direction;
        for (var i = start; i < length; i++)
        {
            var speed = rng.RandomFloat(speedMinMax);
            var randDir = rng.RandomVector3(-0.5f, 0.5f);
            var velocity = Vector3.Normalize(randDir + direction) * speed;
            _data.Set(i, velocity, rng.RandomVector3(-spread, spread), rng.RandomFloat(lifeMinMax));
        }

        _rng = rng;
    }
    //

    internal void InterpolatePosition(NativeView<Vector4> destination, float timeOffset)
    {
        var dst = destination.Reinterpret<Vector256<float>>();
        var velocities = _data.Velocities.Reinterpret<Vector256<float>>().Slice(0, dst.Length);
        var positions = _data.Positions.Reinterpret<Vector256<float>>().Slice(0, dst.Length);

        var vTimeOffset = Vector256.Create(timeOffset);
        foreach (var it in dst.Zip(velocities, positions))
        {
            var vPos = Fma.MultiplyAdd(it.Item2, vTimeOffset, it.Item3);
            it.Item1 = vPos;
        }
    }

    internal void InterpolateVisual(NativeView<ParticleVertex> destination)
    {
        var destSpan = destination.AsSpan();
        var lifeLutIndices = _data.LifeLutIndices;
        ref var lutRef = ref _data.GetLutRef();
        for (int i = 0; i < destSpan.Length; ++i)
        {
            ref var lut = ref Unsafe.Add(ref lutRef, lifeLutIndices[i]);
            destSpan[i] = lut;
        }
    }

    //
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