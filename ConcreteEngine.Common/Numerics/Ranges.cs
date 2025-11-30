namespace ConcreteEngine.Common.Numerics;

public readonly record struct RangeU16
{
    public ushort Offset { get; init; }
    public ushort Length { get; init; }

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

public readonly record struct Range32(int Offset, int Length);