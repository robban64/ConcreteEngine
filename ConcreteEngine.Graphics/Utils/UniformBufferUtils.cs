using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Utils;



public static class UniformBufferUtils
{
    public const nuint MinCapacityBytes = 16u * 1024u; // 16 KiB
    public const nuint DefaultLowerCapacityBytes = 32u * 1024u; // 64 KiB
    public const nuint DefaultMediumCapacityBytes = 512u * 1024u; // 512 KiB
    public const nuint DefaultUpperCapacityBytes = 2u * 1024u * 1024u; // 2 MiB


    private static int _uboOffsetAlign = -1;
    private static nuint _offsetAlign = 16;

    internal static void Init(int uniformBufferOffsetAlignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(uniformBufferOffsetAlignment, 0,
            nameof(uniformBufferOffsetAlignment));

        _uboOffsetAlign = Math.Max(16, uniformBufferOffsetAlignment);
        _offsetAlign = (nuint)_uboOffsetAlign;
    }

    public static nuint GetDefaultCapacity(nuint stride, UboDefaultCapacity defaultCapacity)
    {
        var size = defaultCapacity switch
        {
            UboDefaultCapacity.Lower => DefaultLowerCapacityBytes,
            UboDefaultCapacity.Medium => DefaultMediumCapacityBytes,
            UboDefaultCapacity.Upper => DefaultUpperCapacityBytes
        };
        nuint q = size / stride;
        return (q == 0 ? stride : q * stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint GetCapacityForEntities<T>(int entities) where T : unmanaged, IUniformGpuData
    {
        nuint blockSize   = (nuint)Unsafe.SizeOf<T>();
        nuint uboAlign    = (nuint)_uboOffsetAlign;              
        nuint stride      = (blockSize + (uboAlign - 1)) & ~(uboAlign - 1);  
        return stride * (nuint)(entities);
    }

    public static nuint GetRequiredCapacity(nuint stride, int expectedRecords) =>
        stride * (nuint)Math.Max(1, expectedRecords);

    public static nuint NextCapacity(nuint currentBytes, nuint requiredBytes)
    {
        nuint cap = currentBytes < MinCapacityBytes ? MinCapacityBytes : currentBytes;
        while (cap < requiredBytes) cap *= 2;
        return cap;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint GetClampOffsetAlign() => (nuint)_uboOffsetAlign;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignUp(nuint v, nuint a) => a == 0 ? v : (v + (a - 1)) & ~(a - 1);

    public static nuint StrideOf<T>() where T : unmanaged, IUniformGpuData =>
        AlignUp((nuint)Unsafe.SizeOf<T>(), _offsetAlign);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStd140Aligned<T>() where T : unmanaged, IUniformGpuData => (Unsafe.SizeOf<T>() % 16) == 0;
    public static bool IsStd140SizeAligned(int size) => (size % 16) == 0;

}