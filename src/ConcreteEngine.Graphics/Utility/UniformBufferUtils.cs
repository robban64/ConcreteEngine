using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Graphics.Utility;

public static class UniformBufferUtils
{
    public const int MinCapacityBytes = 16 * 1024; // 16 KiB
    public const int DefaultLowerCapacityBytes = 32 * 1024; // 64 KiB
    public const int DefaultMediumCapacityBytes = 512 * 1024; // 512 KiB
    public const int DefaultUpperCapacityBytes = 2 * 1024 * 1024; // 2 MiB

    public static int UboOffsetAlign { get; private set; }


    internal static void Init(int uniformBufferOffsetAlignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(uniformBufferOffsetAlignment, 0);
        UboOffsetAlign = int.Max(16, uniformBufferOffsetAlignment);
    }

    public static int GetDefaultCapacity(int stride, UboDefaultCapacity defaultCapacity)
    {
        var size = defaultCapacity switch
        {
            UboDefaultCapacity.Lower => DefaultLowerCapacityBytes,
            UboDefaultCapacity.Medium => DefaultMediumCapacityBytes,
            UboDefaultCapacity.Upper => DefaultUpperCapacityBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(defaultCapacity))
        };
        var q = size / stride;
        return q == 0 ? stride : q * stride;
    }

    public static int GetCapacityForEntities<T>(int entities) where T : unmanaged
    {
        var stride = (Unsafe.SizeOf<T>() + (UboOffsetAlign - 1)) & ~(UboOffsetAlign - 1);
        return stride * entities;
    }

    public static int NextCapacity(int currentBytes, int requiredBytes)
    {
        int cap = currentBytes < MinCapacityBytes ? MinCapacityBytes : currentBytes;
        while (cap < requiredBytes) cap *= 2;
        return cap;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStd140Aligned(int stride)  => stride % 16 == 0;


}