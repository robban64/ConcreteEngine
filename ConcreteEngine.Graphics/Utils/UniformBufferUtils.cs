using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Utils;

public sealed class UniformBufferUtils
{
    public const nuint DefaultBytes = 2u * 1024u * 1024u; // 2 MiB
    public const nuint MinBytes            = 16u * 1024u;        // 16 KiB floor

    private static int _uboOffsetAlign = -1;
    private static nuint _offsetAlign = 16;

    internal static void Init(int uniformBufferOffsetAlignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(uniformBufferOffsetAlignment, 0,
            nameof(uniformBufferOffsetAlignment));
        
        _uboOffsetAlign = Math.Max(16, uniformBufferOffsetAlignment);
        _offsetAlign = (nuint)_uboOffsetAlign;
    }
    
    public static nuint GetDefaultCapacity(nuint stride)
    {
        nuint q = DefaultBytes / stride;
        return (q == 0 ? stride : q * stride);
    }
    
    public static nuint GetRequiredCapacity(nuint stride, int expectedRecords)
        => stride * (nuint)Math.Max(1, expectedRecords);
    
    public static nuint NextCapacity(nuint currentBytes, nuint requiredBytes)
    {
        nuint cap = currentBytes < MinBytes ? MinBytes : currentBytes;
        while (cap < requiredBytes) cap *= 2;
        return cap;
    }

    
    public static nuint GetClampOffsetAlign() => (nuint)_uboOffsetAlign;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignUp(nuint v, nuint a) => a == 0 ? v : (v + (a - 1)) & ~(a - 1);

    public static nuint StrideOf<T>() where T : unmanaged, IUniformGpuData =>
        AlignUp((nuint)Unsafe.SizeOf<T>(), _offsetAlign);

    public static bool IsStd140Aligned<T>() where T : unmanaged, IUniformGpuData => (Unsafe.SizeOf<T>() % 16) == 0;
}