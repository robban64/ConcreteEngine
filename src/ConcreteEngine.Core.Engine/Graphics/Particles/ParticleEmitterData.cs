using System.Numerics;
using System.Runtime.CompilerServices;
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

    private static int LifeStrideSum => Unsafe.SizeOf<ParticleLifeState>() + sizeof(float) + sizeof(int);

    private NativeArray<byte> _spatialData;
    private NativeArray<byte> _lifeData;


    public readonly ParticleLut[] Lut = new ParticleLut[LutLength];
    private NativeView<Vector4> _velocities;
    private NativeView<Vector4> _positions;
    private NativeView<ParticleLifeState> _lifeStates;
    private NativeView<byte> _lifeIndices;
    private NativeView<int> _deadIndices;

    public ParticleEmitterData(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);

        var spatialStride = Unsafe.SizeOf<Vector4>() * 2;
        var spatialSize = IntMath.AlignUp(capacity * spatialStride + 1023, 1024);
        _spatialData = NativeArray.AlignedAllocate<byte>(spatialSize, 64);
        _lifeData = NativeArray.Allocate(capacity * LifeStrideSum + 16);

        var builder = new NativeAllocBuilder(_spatialData, 64);
        _velocities = builder.AllocSlice<Vector4>(capacity);
        _positions = builder.AllocSlice<Vector4>(capacity);

        builder = new NativeAllocBuilder(_lifeData);

        _lifeStates = builder.AllocSlice<ParticleLifeState>(capacity);
        _lifeIndices = builder.AllocSlice<byte>(capacity);
        _deadIndices = builder.AllocSlice<int>(capacity);
    }

    public int Capacity => Positions.Length;
    public bool IsNullOrEmpty => _spatialData.IsNullOrEmpty || _lifeData.IsNullOrEmpty;

    public NativeView<Vector4> Velocities => _velocities;
    public NativeView<Vector4> Positions => _positions;
    public NativeView<ParticleLifeState> LifeStates => _lifeStates;
    public NativeView<byte> LifeIndices => _lifeIndices;
    public NativeView<int> DeadIndices => _deadIndices;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVelocity(int index, Vector4 velocity) => _velocities[index] = velocity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(int index, Vector4 position) => _positions[index] = position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLife(int index, float life) => _lifeStates[index] = new ParticleLifeState(life, 1f / life);


    public void ReAlloc(int newCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCount, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newCount, MaxCapacity);

        _spatialData.ReAlloc(newCount * Unsafe.SizeOf<Vector4>() * 2, false);
        _lifeData.ReAlloc(newCount * LifeStrideSum, false);
        _spatialData.Clear();
        _lifeData.Clear();

        var builder = new NativeAllocBuilder(_spatialData);
        _velocities = builder.AllocSlice<Vector4>(newCount);
        _positions = builder.AllocSlice<Vector4>(newCount);

        builder = new NativeAllocBuilder(_lifeData);
        _lifeStates = builder.AllocSlice<ParticleLifeState>(newCount);
        _lifeIndices = builder.AllocSlice<byte>(newCount);
        _deadIndices = builder.AllocSlice<int>(newCount);

        Logger.Log(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
    }

    public void UpdateLutFromParticleParams(ColorRgba startColor, ColorRgba endColor, Vector2 sizeStartEnd)
    {
        var lut = Lut;
        for (int i = 0; i < lut.Length; i++)
        {
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, i / 255f);
            var color = ColorRgba.Lerp(startColor, endColor, (byte)i);
            lut[i] = new ParticleLut(size, color);
        }
    }

    public void Dispose()
    {
        _spatialData.Dispose();
        _lifeData.Dispose();
        _velocities = default;
        _positions = default;
        _lifeStates = default;
        _lifeIndices = default;
        _deadIndices = default;
    }
}