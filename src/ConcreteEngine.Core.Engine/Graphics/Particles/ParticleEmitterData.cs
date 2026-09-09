using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    private static int StrideSum => Unsafe.SizeOf<Vector4>() * 2 + sizeof(float) * 2;
    private static int GetCapacity(int count) => count * StrideSum + (Alignment * 3);

    //
    
    private readonly ParticleVertex[] _lut = new ParticleVertex[LutLength];

    private byte[] _lifeLutIndices = null!;
    
    private NativeArray<byte> _buffer;

    public NativeView<Vector4> Velocities { get; private set; }
    public NativeView<Vector4> Positions { get; private set; }
    public NativeView<float> LifeState { get; private set; }
    public NativeView<float> LifeMaxInverse { get; private set; }


    public ParticleEmitterData(int count)
    {
        EnsureAllocate(count);
    }

    public int Capacity => _buffer.Length;
    public int Count => Velocities.Length;
    public bool IsNullOrEmpty => _buffer.IsNullOrEmpty;

    public Span<byte> GetLutIndices(int count) => _lifeLutIndices.AsSpan(0,count);
    public ref ParticleVertex GetLutRef() => ref MemoryMarshal.GetArrayDataReference(_lut);

    public void UpdateLutFromParticleParams(ColorRgba startColor, ColorRgba endColor, Vector2 sizeStartEnd)
    {
        var lut = _lut;
        for (int i = 0; i < lut.Length; i++)
        {
            var size = float.Lerp(sizeStartEnd.X, sizeStartEnd.Y, i / 255f);
            var color = ColorRgba.Lerp(startColor, endColor, (byte)i);
            lut[i] = new ParticleVertex(size, color);
        }
    }


    internal bool EnsureAllocate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, MinCapacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCapacity);

        count = IntMath.AlignUp(count, CountAlignment);
        var capacity = GetCapacity(count);
        
        var isFirstAlloc = _buffer.IsNull;
        if (!isFirstAlloc && capacity <= _buffer.Length) return false;

        if (isFirstAlloc)
            _buffer = NativeArray.AlignedAllocate(capacity, Alignment);
        else
            _buffer.ReAlloc(capacity, true);

        var allocator = new NativeAllocBuilder(_buffer, alignCursor: Alignment);
        Velocities = allocator.AllocSlice<Vector4>(count);
        Positions = allocator.AllocSlice<Vector4>(count);
        LifeState = allocator.AllocSlice<float>(count);
        LifeMaxInverse = allocator.AllocSlice<float>(count);

        _lifeLutIndices = new byte[count];

        if(!isFirstAlloc) Logger.Log(LogScope.Engine, "ParticleEmitterData: resized", LogLevel.Warn);
        return true;
    }

    public void Dispose()
    {
        _buffer.Dispose();
        Velocities = default;
        Positions = default;
        LifeState = default;
        LifeMaxInverse = default;
    }
}