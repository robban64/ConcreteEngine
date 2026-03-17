namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class BitMath
{
    public static ushort ByteMaskU16(int byteIdx) => (ushort)(0xFFu << (byteIdx * 8));

    public static uint ByteMaskU32(int byteIdx) => 0xFFu << (byteIdx * 8);

    public static ulong ByteMaskU64(int byteIdx) => 0xFFUL << (byteIdx * 8);
}