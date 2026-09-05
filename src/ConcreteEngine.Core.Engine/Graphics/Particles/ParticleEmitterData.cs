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

    public readonly ParticleVisualState[] Lut = new ParticleVisualState[LutLength];

    private NativeSoA<Vector4, Vector4> _spatialData;
    private NativeSoA<float, float, byte> _lifeData;

    public ParticleEmitterData(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCapacity);

        _spatialData = NativeSoA<Vector4, Vector4>.AlignedAllocate(count, 64);
        _lifeData = NativeSoA<float, float, byte>.Allocate(count);
    }

    public int Capacity => _lifeData.Length;

    public bool IsNullOrEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _spatialData.IsNullOrEmpty || _lifeData.IsNullOrEmpty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Vector4> VelocitySpan(int count) => _spatialData.Span1.Slice(0, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<Vector4> PositionSpan(int count) => _spatialData.Span2.Slice(0, count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<float> LifeInvMaxSpan(int start, int count) => _lifeData.Span2.Slice(start, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> LifeIndicesSpan(int start, int count) => _lifeData.Span3.Slice(start, count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Vector4> Velocities(int count) => _spatialData.View1.Slice(0, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<Vector4> Positions(int count) => _spatialData.View2.Slice(0, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<float> Life(int count) => _lifeData.View1.Slice(0, count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<float> LifeInvMax(int count) => _lifeData.View2.Slice(0, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> LifeIndices(int count) => _lifeData.View3.Slice(0, count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Vector4 GetVelocity(int index) => ref _spatialData.At1(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Vector4 GetPosition(int index) => ref _spatialData.At2(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLife(int index, float life)
    {
        _lifeData.At1(index) = life;
        _lifeData.At2(index) = 1f / life;
    }

    
    public void UpdateLutFromParticleParams(ColorRgba startColor, ColorRgba endColor, Vector2 sizeStartEnd)
    {
        var lut = Lut;
        for (int i = 0; i < lut.Length; i++)
        {
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, i / 255f);
            var color = ColorRgba.Lerp(startColor, endColor, (byte)i);
            lut[i] = new ParticleVisualState(size, color);
        }
    }

    public PtrEnumerator<float, float, byte> LifeEnumerator(int count) =>
        new(Life(count), LifeInvMax(count), LifeIndices(count));

    public void ReAlloc(int newCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(newCount, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newCount, MaxCapacity);

        _spatialData.ReAlloc(newCount, false);
        _lifeData.ReAlloc(newCount, false);
        _spatialData.Clear();
        _lifeData.Clear();

        Logger.Log(LogScope.Engine, "ParticleEmitter: resized", LogLevel.Warn);
    }

    
    public void Dispose()
    {
        _spatialData.Dispose();
        _lifeData.Dispose();
    }
}