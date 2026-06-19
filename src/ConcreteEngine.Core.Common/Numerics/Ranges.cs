using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics;

public interface IRange
{
    int Offset { get; }
    int Length { get; }
}

public readonly record struct RangeU16(ushort Offset16, ushort Length16) : IRange
{
    public int Offset => Offset16;
    public int Length => Length16;

    public int End
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Offset + Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RangeU16(int offset, int length) : this((ushort)offset, (ushort)length) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RangeU16((int, int) it) => new(it.Item1, it.Item2);
}

public readonly record struct Range32(int Offset, int Length) : IRange
{
    public int End
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Offset + Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Range32((int, int) it) => new(it.Item1, it.Item2);
}