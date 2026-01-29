using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics;

public interface ISlotRange
{
    int Offset { get; }
    int Length { get; }
}

public readonly record struct RangeU16 : ISlotRange
{

    private readonly ushort _uOffset;
    private readonly ushort _uLength;

    public int Offset => _uOffset;
    public int Length =>  _uLength;
    public int End => _uOffset + _uLength;

    public RangeU16(int offset, int length)
    {
        _uOffset = (ushort)offset;
        _uLength = (ushort)length;
    }

    public RangeU16(ushort Offset, ushort Length)
    {
        _uOffset = Offset;
        _uLength = Length;
    }
    
    public static implicit operator RangeU16((int, int) it) => new(it.Item1, it.Item2);
}

public readonly record struct Range32(int Offset, int Length) : ISlotRange
{
    public int End => Offset + Length;
    public static implicit operator Range32((int, int) it) => new(it.Item1, it.Item2);
}