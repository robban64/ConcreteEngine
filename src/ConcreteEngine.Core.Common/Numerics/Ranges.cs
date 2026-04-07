namespace ConcreteEngine.Core.Common.Numerics;

public interface IRange
{
    int Offset { get; }
    int Length { get; }
}

public readonly record struct RangeU16 : IRange
{
    public readonly ushort Offset16;
    public readonly ushort Length16;

    public int Offset => Offset16;
    public int Length => Length16;
    public int End => Offset16 + Length16;

    public RangeU16(int offset, int length)
    {
        Offset16 = (ushort)offset;
        Length16 = (ushort)length;
    }

    public RangeU16(ushort Offset, ushort Length)
    {
        Offset16 = Offset;
        Length16 = Length;
    }

    public static implicit operator RangeU16((int, int) it) => new(it.Item1, it.Item2);
}

public readonly record struct Range32(int Offset, int Length) : IRange
{
    public int End => Offset + Length;
    public static implicit operator Range32((int, int) it) => new(it.Item1, it.Item2);
}