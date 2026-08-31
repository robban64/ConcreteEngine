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

internal sealed unsafe class ParticleEmitterData : IDisposable
{
    private const int LutLength = 256;
    public const int MinCapacity = 128;
    public const int MaxCapacity = 8192;

    private NativeArray<ParticleState> _particleState;
    private NativeArray<Vector2> _particleLifeState;
    private NativeArray<float> _particleInvLifeState;
    private NativeArray<int> _deadIndices;

    public readonly ParticleLut[] Lut = new ParticleLut[LutLength];

    public ParticleEmitterData(int capacity)
    {
        if (Unsafe.SizeOf<Vector2>() != Unsafe.SizeOf<ParticleLifeState>()) Throwers.InvalidOperation();

        _particleState = NativeArray.Allocate<ParticleState>(capacity, zeroed: true);
        _particleLifeState = NativeArray.Allocate<Vector2>(capacity, zeroed: true);
        _particleInvLifeState = NativeArray.Allocate<float>(capacity, zeroed: true);
        _deadIndices = NativeArray.Allocate<int>(capacity, zeroed: true);
    }

    public int Capacity => _particleState.Length;

    public bool HasNullData => _particleState.IsNull || _particleLifeState.IsNull || _particleInvLifeState.IsNull ||
                               _deadIndices.IsNull;

    public NativeView<ParticleState> ParticleState => _particleState;

    public NativeView<ParticleLifeState> ParticleLifeState =>
        _particleLifeState.AsView().Reinterpret<ParticleLifeState>();

    public NativeView<float> ParticleInvLifeState => _particleInvLifeState;
    public NativeView<int> DeadIndices => _deadIndices;

    public void ReAlloc(int newCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCapacity, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newCapacity, MaxCapacity);

        _particleState.ReAlloc(newCapacity, true);
        _particleInvLifeState.ReAlloc(newCapacity, true);
        _particleLifeState.ReAlloc(newCapacity, true);
        _deadIndices.ReAlloc(newCapacity, true);
        Logger.Log(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _particleState.Dispose();
        _particleLifeState.Dispose();
        _particleInvLifeState.Dispose();
        _deadIndices.Dispose();
    }
}

[Inspect]
public sealed class ParticleEmitter : IComparable<ParticleEmitter>, IComparable<ushort>, IDisposable
{
    private const int LutLength = 256;
    public const int MinCapacity = 128;
    public const int MinCount = 16;
    public const int MaxCount = 8192;

    private bool _isDirty;
    private FastRandom _rng;

    private readonly ParticleEmitterData _data;

    public readonly Id16<ParticleEmitter> Id;

    public readonly string Name;

    public MeshId BoundMesh { get; private set; }
    public int BoundSlot { get; private set; } = -1;

    public int ParticleCount { get; private set; }
    public int PendingParticleCount { get; private set; }


    private ParticleParams _particleParams;
    private EmitterParams _emitterParams;
    [Segment("Simulation")] [InputNumber] public Vector3 Gravity = new Vector3(0.0f, 0.015f, 0.0f);
    [Segment("Ambient")] [InputNumber] public float Drag;

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
        _emitterParams = emitterParams;
        _particleParams = particleParams;
        ParticleCount = PendingParticleCount = particleCount;
        _rng = new FastRandom((uint)Environment.TickCount + Id.Id);

        var length = int.Max(MinCapacity, IntMath.AlignUp(particleCount, 128));
        _data = new ParticleEmitterData(length);
        InitializeParticles(0, ParticleCount);
    }

    public bool IsDirty => _isDirty;
    public bool IsAttached => BoundSlot >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly EmitterParams GetEmitterParams() => ref _emitterParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ParticleParams GetParticleParams() => ref _particleParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BoundingBox LocalBounds() => ref _localBounds;

    //TEMP
    [InspectInclude]
    public ref EmitterParams EmitterParams => ref _emitterParams;

    [InspectInclude]
    public ref ParticleParams ParticleParams => ref _particleParams;
    //

    internal void Attach(int slot, MeshId meshId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfZero(meshId.Id);
        if (BoundSlot >= 0) throw new ArgumentOutOfRangeException(nameof(slot));
        BoundSlot = slot;
        BoundMesh = meshId;
        UpdateLutFromParticleParams();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ParticleEmitterData GetEmitterData()
    {
        if (_data.HasNullData)
            Throwers.InvalidOperation("ParticleEmitter: invalid particle data");

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

        if (PendingParticleCount == ParticleCount) return;

        var newCapacity = int.Max(MinCapacity, IntMath.AlignUp(PendingParticleCount, 128));
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


    private void UpdateLutFromParticleParams()
    {
        _particleParams.Deconstruct(out var startColor, out var endColor, out var sizeStartEnd);
        var lut = _data.Lut;
        for (int i = 0; i < LutLength; i++)
        {
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, i / 255f);
            var color = ColorRgba.Lerp(startColor, endColor, (byte)i);
            lut[i] = new ParticleLut(size, color);
        }
    }

    [SkipLocalsInit]
    internal void Simulate(float simDt)
    {
        // avg1.BeginSample();
        var dead = SimLife(simDt);
        // if (avg1.EndSample() > 80) avg1.ResetAndPrint("SimLife");
        if (dead > 0)
        {
            // avg2.BeginSample();
            SimDead(_data.DeadIndices.Slice(0, dead));
            //if (avg2.EndSample() > 80) avg2.ResetAndPrint("SimDead");
        }

        //avg3.BeginSample();
        Sim2(simDt);
        // if (avg3.EndSample() > 80) avg3.ResetAndPrint("SimPosition");
    }

    //private static AvgFrameTimer avg1, avg2, avg3;

    private ref readonly Vector2 SpeedMinMax => ref _emitterParams.SpeedMinMax;
    private ref readonly Vector2 LifeMinMax => ref _emitterParams.LifeMinMax;

    [SkipLocalsInit]
    private unsafe void SimDead(NativeView<int> deadIndices)
    {
        var direction = new Vector4(_emitterParams.Direction, 0);
        var spread = _emitterParams.Spread;

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

    private unsafe int SimLife(float simDt)
    {
        int index = 0;
        var deadIndices = _data.DeadIndices.Ptr;
        foreach (var it in _data.ParticleLifeState.Zip(_data.ParticleInvLifeState))
        {
            var isAlive = it.Item1.Life > 0f;
            if (isAlive)
            {
                var p = it.Item1.Life * it.Item1.LifeInvMax;
                it.Item2 = float.Clamp(1f - p, 0f, 1f);
                it.Item1.Life -= simDt;
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
    private void Sim2(float simDt)
    {
        var deltaVec4 = new Vector4(simDt);
        var gravityStep = new Vector4(Gravity * simDt, 0f);
        foreach (ref var p in _data.ParticleState)
        {
            var velocity = p.Velocity + gravityStep;
            p.Velocity = velocity;
            p.Position = Vector4.FusedMultiplyAdd(velocity, deltaVec4, p.Position);
        }
    }

    private void InitializeParticles(int start, int length)
    {
        if ((uint)start + (uint)length > (uint)_data.Capacity)
            Throwers.RangeOutOfBounds(start, length, _data.Capacity);

        var rng = _rng;
        var lifeMinMax = _emitterParams.LifeMinMax;
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ColorRgba LerpSse(ColorRgba a, ColorRgba b, byte t)
    {
        var va = Sse41.ConvertToVector128Int32(
            Vector128.CreateScalarUnsafe(Unsafe.As<ColorRgba, uint>(ref a)).AsByte());
        var vb = Sse41.ConvertToVector128Int32(
            Vector128.CreateScalarUnsafe(Unsafe.As<ColorRgba, uint>(ref b)).AsByte());

        var vt = Vector128.Create((int)t);

        var lerped = Sse2.Add(va, Sse2.ShiftRightArithmetic(Sse41.MultiplyLow(Sse2.Subtract(vb, va), vt), 8));

        var packed = Sse2.PackUnsignedSaturate(
            Sse2.PackSignedSaturate(lerped, Vector128<int>.Zero),
            Vector128<short>.Zero);

        uint scalar = packed.AsUInt32().ToScalar();
        return Unsafe.As<uint, ColorRgba>(ref scalar);
    }
}