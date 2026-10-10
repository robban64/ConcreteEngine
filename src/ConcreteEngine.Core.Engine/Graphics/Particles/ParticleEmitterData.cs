using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

internal sealed class ParticleEmitterData : IDisposable
{
    public const int LutLength = 256;
    public const int MinCapacity = 128;
    public const int MaxCapacity = 8192;

    public const int Alignment = 64;
    public const int CountAlignment = 16;

    private static int GetCapacity(int count) => count * StrideSum + (Alignment * 4);
    private static int StrideSum => Unsafe.SizeOf<Vector4>() * 2 + sizeof(float) * 2 + sizeof(byte);

    //

    public int Capacity { get; private set; }

    private readonly ParticleVertex[] _lut = new ParticleVertex[LutLength];

    private NativeArray<byte> _buffer;

    private NativeView<Vector4> _velocities;
    private NativeView<Vector4> _positions;
    private NativeView<float> _lifeState;
    private NativeView<float> _lifeMaxInverse;
    private NativeView<byte> _lifeLutIndices;

    private FastRandom _rng = new(1337);

    public ParticleEmitterData(int count)
    {
        EnsureAllocate(count);
    }

    public NativeView<Vector4> Velocities => _velocities;
    public NativeView<Vector4> Positions => _positions;
    public NativeView<float> LifeState => _lifeState;
    public NativeView<float> LifeMaxInverse => _lifeMaxInverse;
    public NativeView<byte> LifeLutIndices => _lifeLutIndices;

    public int SizeInBytes => _buffer.Length;
    public bool IsNullOrEmpty => Capacity == 0 || _buffer.IsNullOrEmpty;


    public void UpdateLut(ColorRgba startColor, ColorRgba endColor, Vector2 sizeStartEnd)
    {
        var lut = _lut;
        for (int i = 0; i < lut.Length; i++)
        {
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, i / 255f);
            var color = ColorRgba.Lerp(startColor, endColor, (byte)i);
            lut[i] = new ParticleVertex(size, color);
        }
    }
    
    public void Respawn(int index, ParticleEmitterState state)
    {
        var random = _rng;
        var spread = state.Spread;

        var randDir = random.RandomVector3(-0.5f, 0.5f);
        var speed = random.RandomFloat(state.SpeedMinMax);
        var velocity = Vector3.Normalize(randDir + state.Direction) * speed;
        var position = random.RandomVector3(-spread, spread);
        var life = random.RandomFloat(state.LifeMinMax);
        Set(index, velocity, position, life);
        _rng = random;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Vector3 velocity, Vector3 position, float life)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)Capacity);
        _velocities[index] = velocity.AsVector4();
        _positions[index] = position.AsVector4();
        _lifeState[index] = life;
        _lifeMaxInverse[index] = 1f / life;
        _lifeLutIndices[index] = 0;
    }


    public void RespawnParticles(int deadCount, ParticleEmitterState state, Span<Bit64> deadBits)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)deadCount, (uint)Capacity);

        for (int start = 0; start < deadCount; start += 64)
        {
            var block = deadBits[start >> 6];
            if (block == 0) continue;
            foreach (var p in block) Respawn(start + p, state);
        }
    }


    public unsafe int SimulateLife(int count, float simDt, Span<Bit64> deadBits)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)count, (uint)Capacity);

        var lifeState = LifeState;
        var invMaxLifeState = LifeMaxInverse;
        var lutIndices = LifeLutIndices;

        var deadIndex = 0;

        var length = count - Vector256<float>.Count;
        for (int i = 0; i <= length; i += Vector256<float>.Count)
        {
            var life = lifeState + i;
            var invMaxLife = invMaxLifeState + i;
            ref var lut = ref Unsafe.As<byte, long>(ref lutIndices[i]);

            var vLife = Vector256.Subtract(Vector256.LoadAligned(life), Vector256.Create(simDt));
            var vDiff = Fma.MultiplyAddNegated(vLife, Vector256.LoadAligned(invMaxLife), Vector256<float>.One);
            var vIndex = Fma.MultiplyAdd(vDiff, Vector256.Create(255f), Vector256.Create(0.5f));

            var vIndexInt32 = Avx.ConvertToVector256Int32(vIndex);
            var bytes = Sse2.PackUnsignedSaturate(
                Sse2.PackSignedSaturate(vIndexInt32.GetLower(), vIndexInt32.GetUpper()),
                Vector128<short>.Zero
            );

            var mask = Vector256.LessThanOrEqual(vLife, Vector256<float>.Zero).ExtractMostSignificantBits();

            vLife.StoreAligned(life);
            lut = bytes.AsInt64().ToScalar();
            if (mask != 0) deadIndex = UpdateDeadBits(i, mask, deadBits);
        }

        return deadIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int UpdateDeadBits(int i, uint bitMask, Span<Bit64> deadBits)
        {
            ref var blockRef = ref deadBits[i >> 6];

            var block = blockRef;
            var bitIndex = 0;
            var mask = bitMask;
            while (mask != 0)
            {
                var index = BitOperations.TrailingZeroCount(mask) + i;
                mask &= mask - 1;

                bitIndex = index;
                block.Enable(index);
            }

            blockRef = block;
            return bitIndex;
        }
    }

    public unsafe void SimulateSpatial(int count, Vector3 gravity, float simDt)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)count, (uint)Capacity);

        var velocities = Velocities.Slice(0, count).Reinterpret<float>();
        var positions = Positions.Slice(0, count).Reinterpret<float>();
        var length = velocities.Length - Vector256<float>.Count;

        var vGravityStep = Vector256.Create(gravity.AsVector128() * simDt);
        for (int i = 0; i < length; i += Vector256<float>.Count)
        {
            var velocity = velocities + i;
            var position = positions + i;
            var vVelocity = Vector256.Add(Vector256.LoadAligned(velocity), vGravityStep);
            var vPosition = Fma.MultiplyAdd(vVelocity, Vector256.Create(simDt), Vector256.LoadAligned(position));
            vVelocity.StoreAligned(velocity);
            vPosition.StoreAligned(position);
        }
    }

    //
    public unsafe void InterpolatePosition(NativeView<Vector4> destination, float timeOffset)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)destination.Length, (uint)Capacity);

        var dst = destination.Reinterpret<float>();
        var velocities = Velocities.Reinterpret<float>();
        var positions = Positions.Reinterpret<float>();
        var length = dst.Length - Vector256<float>.Count;
        
        var vTimeOffset = Vector256.Create(timeOffset);
        for (int i = 0; i < length; i += Vector256<float>.Count)
        {
            var vVelocity = Vector256.LoadAligned(velocities + i);
            var vPosition = Vector256.LoadAligned(positions + i);
            var vPos = Fma.MultiplyAdd(vVelocity, vTimeOffset, vPosition);
            vPos.StoreAligned(dst + i);
        }
    }

    public void InterpolateVisual(NativeView<ParticleVertex> destination)
    {
        var count = destination.Length;
        var destSpan = destination.AsSpan(0, count);
        var lifeLutIndices = _lifeLutIndices.AsSpan(0, count);
        
        if (destSpan.Length != lifeLutIndices.Length) Throwers.InvalidOperation();

        var lut = _lut;
        for (int i = 0; i < count; ++i)
        {
            destSpan[i] = lut[lifeLutIndices[i]];
        }
    }
    //

    public bool EnsureAllocate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, MaxCapacity);

        var count = IntMath.AlignUp(length, CountAlignment);
        var capacity = GetCapacity(count);

        var isFirstAlloc = _buffer.IsNull;
        if (!isFirstAlloc && capacity <= _buffer.Length) return false;

        if (isFirstAlloc)
            _buffer = NativeArray.AlignedAllocate(capacity, Alignment);
        else
            _buffer.ReAlloc(capacity, true);

        var allocator = new NativeAllocBuilder(_buffer, alignCursor: Alignment);
        _velocities = allocator.AllocSlice<Vector4>(count);
        _positions = allocator.AllocSlice<Vector4>(count);
        _lifeState = allocator.AllocSlice<float>(count);
        _lifeMaxInverse = allocator.AllocSlice<float>(count);
        _lifeLutIndices = allocator.AllocSlice<byte>(count);

        Capacity = count;

        if (!isFirstAlloc) Logger.Log(LogScope.Engine, "ParticleEmitterData: resized", LogLevel.Warn);
        return true;
    }

    public void Dispose()
    {
        _buffer.Dispose();
        _velocities = default;
        _positions = default;
        _lifeState = default;
        _lifeMaxInverse = default;
        _lifeLutIndices = default;

        Capacity = 0;
    }
}