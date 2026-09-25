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

    private static int StrideSum => Unsafe.SizeOf<Vector4>() * 2 + sizeof(float) * 2 + sizeof(byte);
    private static int GetCapacity(int count) => count * StrideSum + (Alignment * 4);

    //

    private readonly ParticleVertex[] _lut = new ParticleVertex[LutLength];

    private NativeArray<byte> _buffer;

    private NativeView<Vector4> _velocities;
    private NativeView<Vector4> _positions;
    private NativeView<float> _lifeState;
    private NativeView<float> _lifeMaxInverse;
    private NativeView<byte> _lifeLutIndices;


    public ParticleEmitterData(int count)
    {
        EnsureAllocate(count);
    }
    
    public NativeView<Vector4> Velocities => _velocities;
    public NativeView<Vector4> Positions => _positions;
    public NativeView<float> LifeState => _lifeState;
    public NativeView<float> LifeMaxInverse => _lifeMaxInverse;
    public NativeView<byte> LifeLutIndices => _lifeLutIndices;

    public int Capacity => _buffer.Length;
    public int Count => _velocities.Length;
    public bool IsNullOrEmpty => _buffer.IsNullOrEmpty;

    public ref ParticleVertex GetLutRef() => ref MemoryMarshal.GetArrayDataReference(_lut);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, Vector3 velocity, Vector3 position, float life)
    {
        _velocities[index] = velocity.AsVector4();
        _positions[index] = position.AsVector4();
        _lifeState[index] = life;
        _lifeMaxInverse[index] = 1f / life;
    }
    
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
        _velocities = allocator.AllocSlice<Vector4>(count);
        _positions = allocator.AllocSlice<Vector4>(count);
        _lifeState = allocator.AllocSlice<float>(count);
        _lifeMaxInverse = allocator.AllocSlice<float>(count);
        _lifeLutIndices = allocator.AllocSlice<byte>(count);

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
    }
}