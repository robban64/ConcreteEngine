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

    private static int LifeStrideSum => Unsafe.SizeOf<ParticleLifeState>() + sizeof(float) + sizeof(int);

    private NativeArray<byte> _spatialData;
    private NativeArray<byte> _lifeData;

    public NativeView<ParticleState> Spatial { get; private set; }
    public NativeView<ParticleLifeState> LifeStates { get; private set; }
    public NativeView<byte> LifeIndices { get; private set; }
    public NativeView<int> DeadIndices { get; private set; }


    public readonly ParticleLut[] Lut = new ParticleLut[LutLength];

    public ParticleEmitterData(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);
        
        _spatialData = NativeArray.AlignedAllocate<byte>(capacity * Unsafe.SizeOf<ParticleState>(), 32);
        Spatial = _spatialData.Reinterpret<ParticleState>();
       
        _lifeData = NativeArray.Allocate(capacity * LifeStrideSum);
        var builder = new NativeAllocBuilder(_lifeData);

        LifeStates = builder.AllocSlice<ParticleLifeState>(capacity);
        LifeIndices = builder.AllocSlice<byte>(capacity);
        DeadIndices = builder.AllocSlice<int>(capacity);
    }

    public int Capacity => Spatial.Length;
    public bool IsNullOrEmpty => _spatialData.IsNullOrEmpty || _lifeData.IsNullOrEmpty;

    
    public void ReAlloc(int newCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCount, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newCount, MaxCapacity);

        _spatialData.ReAlloc(newCount, false);
        _lifeData.ReAlloc(newCount, false);
        _spatialData.Clear();
        _lifeData.Clear();

        Spatial = _spatialData.Reinterpret<ParticleState>();
       
        var builder = new NativeAllocBuilder(_lifeData);
        LifeStates = builder.AllocSlice<ParticleLifeState>(newCount);
        LifeIndices = builder.AllocSlice<byte>(newCount);
        DeadIndices = builder.AllocSlice<int>(newCount);

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
        _spatialData.Dispose();
        _lifeData.Dispose();
        Spatial = default;
        LifeStates = default;
        LifeIndices = default;
        DeadIndices = default;

    }
}
