namespace ConcreteEngine.Common.Numerics;

public readonly record struct RangeU16
{
    public readonly ushort Offset;
    public readonly ushort Length;
    
    public static implicit operator RangeU16((int, int) it) => new (it.Item1, it.Item2);

    public RangeU16(ushort offset, ushort length)
    {
        Offset = offset;
        Length = length;
    }

    public RangeU16(int offset, int length)
    {
        Offset = (ushort)offset;
        Length = (ushort)length;
    }
}

public readonly record struct Range32
{
    public readonly int Offset;
    public readonly int Length;

    public static implicit operator Range32((int, int) it) => new (it.Item1, it.Item2);

    public Range32(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }

}