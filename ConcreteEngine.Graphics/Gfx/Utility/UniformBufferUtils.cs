#region

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Utility;

public static class UniformBufferUtils
{
    public const nint MinCapacityBytes = 16 * 1024; // 16 KiB
    public const nint DefaultLowerCapacityBytes = 32 * 1024; // 64 KiB
    public const nint DefaultMediumCapacityBytes = 512 * 1024; // 512 KiB
    public const nint DefaultUpperCapacityBytes = 2 * 1024 * 1024; // 2 MiB


    private static nint _uboOffsetAlign = 0;
    private static nint _offsetAlign = 16;

    public static nint UboOffsetAlign
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _uboOffsetAlign;
    }


    internal static void Init(int uniformBufferOffsetAlignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(uniformBufferOffsetAlignment, 0,
            nameof(uniformBufferOffsetAlignment));

        _uboOffsetAlign = Math.Max(16, uniformBufferOffsetAlignment);
        _offsetAlign = _uboOffsetAlign;
    }

    public static nint GetDefaultCapacity(nint stride, UboDefaultCapacity defaultCapacity)
    {
        var size = defaultCapacity switch
        {
            UboDefaultCapacity.Lower => DefaultLowerCapacityBytes,
            UboDefaultCapacity.Medium => DefaultMediumCapacityBytes,
            UboDefaultCapacity.Upper => DefaultUpperCapacityBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(defaultCapacity))
        };
        nint q = size / stride;
        return q == 0 ? stride : q * stride;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetCapacityForEntities<T>(int entities) where T : unmanaged, IStd140Uniform
    {
        nint blockSize = Unsafe.SizeOf<T>();
        nint uboAlign = _uboOffsetAlign;
        nint stride = (blockSize + (uboAlign - 1)) & ~(uboAlign - 1);
        return stride * entities;
    }

    public static nint GetRequiredCapacity(nint stride, int expectedRecords) => stride * Math.Max(1, expectedRecords);

    public static nint NextCapacity(nint currentBytes, nint requiredBytes)
    {
        nint cap = currentBytes < MinCapacityBytes ? MinCapacityBytes : currentBytes;
        while (cap < requiredBytes) cap *= 2;
        return cap;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint AlignUp(nint v, nint a) => a == 0 ? v : (v + (a - 1)) & ~(a - 1);

    public static nint StrideOf<T>() where T : unmanaged, IStd140Uniform => AlignUp(Unsafe.SizeOf<T>(), _offsetAlign);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStd140Aligned<T>() where T : unmanaged, IStd140Uniform => Unsafe.SizeOf<T>() % 16 == 0;


    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    [StackTraceHidden]
    private static void ThrowStd140NotAligned<T>() where T : unmanaged =>
        throw new GraphicsException($"Invalid struct layout: {Unsafe.SizeOf<T>()} bytes for {typeof(T).Name}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsStd140AlignedOrThrow<T>(out nint stride) where T : unmanaged, IStd140Uniform
    {
        stride = (nint)Unsafe.SizeOf<T>();
        if (stride % 16 != 0) ThrowStd140NotAligned<T>();
    }
}