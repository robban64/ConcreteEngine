using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Utility;

public static class UniformBufferUtils
{
    public const uint MinCapacityBytes = 16 * 1024; // 16 KiB
    public const uint DefaultLowerCapacityBytes = 32 * 1024; // 64 KiB
    public const uint DefaultMediumCapacityBytes = 512 * 1024; // 512 KiB
    public const uint DefaultUpperCapacityBytes = 2 * 1024 * 1024; // 2 MiB


    public static uint UboOffsetAlign { get; private set; }


    internal static void Init(int uniformBufferOffsetAlignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(uniformBufferOffsetAlignment, 0,
            nameof(uniformBufferOffsetAlignment));

        UboOffsetAlign = (uint)int.Max(16, uniformBufferOffsetAlignment);
    }

    public static uint GetDefaultCapacity(uint stride, UboDefaultCapacity defaultCapacity)
    {
        var size = defaultCapacity switch
        {
            UboDefaultCapacity.Lower => DefaultLowerCapacityBytes,
            UboDefaultCapacity.Medium => DefaultMediumCapacityBytes,
            UboDefaultCapacity.Upper => DefaultUpperCapacityBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(defaultCapacity))
        };
        uint q = size / stride;
        return q == 0 ? stride : q * stride;
    }

    public static uint GetCapacityForEntities<T>(int entities) where T : unmanaged
    {
        uint blockSize = (uint)Unsafe.SizeOf<T>();
        uint uboAlign = UboOffsetAlign;
        uint stride = (blockSize + (uboAlign - 1)) & ~(uboAlign - 1);
        return stride * (uint)entities;
    }

    public static uint GetRequiredCapacity(int stride, int expectedRecords) =>
        (uint)stride * (uint)int.Max(1, expectedRecords);

    public static uint NextCapacity(uint currentBytes, uint requiredBytes)
    {
        uint cap = currentBytes < MinCapacityBytes ? MinCapacityBytes : currentBytes;
        while (cap < requiredBytes) cap *= 2;
        return cap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStd140Aligned<T>() where T : unmanaged => Unsafe.SizeOf<T>() % 16 == 0;


    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowStd140NotAligned<T>() where T : unmanaged =>
        throw new GraphicsException($"Invalid struct layout: {Unsafe.SizeOf<T>()} bytes for {typeof(T).Name}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IsStd140AlignedOrThrow<T>(out nint stride) where T : unmanaged
    {
        stride = Unsafe.SizeOf<T>();
        if (stride % 16 != 0) ThrowStd140NotAligned<T>();
    }
}