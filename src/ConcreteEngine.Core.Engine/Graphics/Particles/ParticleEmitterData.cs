using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

internal sealed class ParticleEmitterData : IDisposable
{
    public const int LutLength = 256;
    public const int MinCapacity = 128;
    public const int MaxCapacity = 8192;

    private int StrideSum =>
        Unsafe.SizeOf<ParticleState>() + Unsafe.SizeOf<ParticleLifeState>() + sizeof(float) + sizeof(int);

    private NativeArray<byte> _data;

    private NativeView<ParticleState> _particleState;
    private NativeView<ParticleLifeState> _particleLifeState;
    private NativeView<byte> _particleInvLifeState;
    private NativeView<int> _deadIndices;

    public readonly ParticleLut[] Lut = new ParticleLut[LutLength];

    public ParticleEmitterData(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);
        
        var sizeInBytes = capacity * StrideSum;
        _data = NativeArray.Allocate(sizeInBytes);

        var builder = new NativeAllocBuilder(_data);
        _particleState = builder.AllocSlice<ParticleState>(capacity);
        _particleLifeState = builder.AllocSlice<ParticleLifeState>(capacity);
        _particleInvLifeState = builder.AllocSlice<byte>(capacity);
        _deadIndices = builder.AllocSlice<int>(capacity);
    }

    public int Capacity => _particleState.Length;
    public bool IsNullOrEmpty => _data.IsNullOrEmpty || _particleState.IsNullOrEmpty;

    public NativeView<ParticleState> ParticleState => _particleState;
    public NativeView<ParticleLifeState> ParticleLifeState => _particleLifeState;
    public NativeView<byte> ParticleInvLifeState => _particleInvLifeState;
    public NativeView<int> DeadIndices => _deadIndices;

    public void ReAlloc(int newCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCount, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newCount, MaxCapacity);

        var sizeInBytes = newCount * StrideSum;
        _data.ReAlloc(sizeInBytes, false);
        _data.Clear();

        var builder = new NativeAllocBuilder(_data);
        _particleState = builder.AllocSlice<ParticleState>(newCount);
        _particleLifeState = builder.AllocSlice<ParticleLifeState>(newCount);
        _particleInvLifeState = builder.AllocSlice<byte>(newCount);
        _deadIndices = builder.AllocSlice<int>(newCount);
        Logger.Log(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
    }

    public void UpdateLutFromParticleParams(ColorRgba startColor,  ColorRgba endColor,  Vector2 sizeStartEnd)
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
        _data.Dispose();
        _particleState = default;
        _particleLifeState = default;
        _particleInvLifeState = default;
        _deadIndices = default;

    }
}
