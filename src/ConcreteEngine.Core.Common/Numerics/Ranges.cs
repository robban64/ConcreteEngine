namespace ConcreteEngine.Core.Common.Numerics;

public interface IRange
{
    int Offset { get; }
    int Length { get; }
}

public readonly record struct RangeU16 : IRange
{
    public readonly ushort UOffset;
    public readonly ushort ULength;

    public int Offset => UOffset;
    public int Length => ULength;
    public int End => UOffset + ULength;

    public RangeU16(int offset, int length)
    {
        UOffset = (ushort)offset;
        ULength = (ushort)length;
    }

    public RangeU16(ushort Offset, ushort Length)
    {
        UOffset = Offset;
        ULength = Length;
    }

    public static implicit operator RangeU16((int, int) it) => new(it.Item1, it.Item2);
}

public readonly record struct Range32(int Offset, int Length) : IRange
{
    public int End => Offset + Length;
    public static implicit operator Range32((int, int) it) => new(it.Item1, it.Item2);
}