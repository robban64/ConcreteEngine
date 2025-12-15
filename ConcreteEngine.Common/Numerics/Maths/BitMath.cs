using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Numerics.Maths;

public static class BitMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ByteMaskU32(int byteIdx) => 0xFFu << (byteIdx * 8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ByteMaskU64(int byteIdx) => 0xFFUL << (byteIdx * 8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ByteMaskU16(int byteIdx) => (ushort)(0xFFu << (byteIdx * 8));
}