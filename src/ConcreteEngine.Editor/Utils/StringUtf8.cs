using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

public unsafe struct FieldTextUtf8
{
    public const int Capacity = 63;

    private fixed byte _value[Capacity];
    private readonly byte _valueStart;

    public ref byte GetLabel() => ref _value[0];
    public ref byte GetValue() => ref _value[_valueStart];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);

    public FieldTextUtf8(ReadOnlySpan<char> label, ReadOnlySpan<char> value)
    {
        _valueStart = 0;

        var span = AsSpan();

        var labelBytes = UtfText.WriteCharSpanSafe(label, span);
        span[labelBytes] = 0;

        _valueStart = (byte)(labelBytes + 1);

        var valueSpan = span.Slice(_valueStart);
        int valueBytes = UtfText.WriteCharSpanSafe(value, valueSpan);
        valueSpan[valueBytes] = 0;
    }
}


internal unsafe struct SearchStringUtf8
{
    public const int Length = 8;

    private fixed byte _value[Length];
    private fixed char _searchString[Length];

    public ref byte GetInputRef() => ref _value[0];

    public Span<char> GetSearchString(out ulong key, out ulong mask)
    {
        key = 0;
        mask = 0;

        var length = UtfText.SliceNullTerminate(MemoryMarshal.CreateSpan(ref _value[0], Length), out var byteSpan);
        var dst = MemoryMarshal.CreateSpan(ref _searchString[0], length);
        if (!InputTextUtils.DecodeUtf8Input(byteSpan, dst, out var searchStr))
            return searchStr;

        key = StringPacker.PackUtf8(byteSpan);
        mask = StringPacker.GetMaskUtf8(length);
        return searchStr;
    }
}