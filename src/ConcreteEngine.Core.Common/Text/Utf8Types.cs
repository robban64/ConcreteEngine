using System.Runtime.InteropServices;
using System.Text;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct String64Utf8
{
    public const int Length = 64;

    public String64Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        span.Slice(0,int.Min(span.Length, Length)).CopyTo(AsSpan());
    }

    public String64Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        Encoding.UTF8.GetBytes(span.Slice(0,int.Min(span.Length, Length)), AsSpan());
    }

    private fixed byte _value[Length];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Length);
}

public unsafe struct String32Utf8
{
    public const int Length = 32;

    public String32Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        span.Slice(0,int.Min(span.Length, Length)).CopyTo(AsSpan());
    }

    public String32Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        Encoding.UTF8.GetBytes(span.Slice(0,int.Min(span.Length, Length)), AsSpan());
    }

    private fixed byte _value[Length];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Length);
}

public unsafe struct String16Utf8
{
    public const int Length = 16;

    public String16Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        span.Slice(0,int.Min(span.Length, Length)).CopyTo(AsSpan());
    }

    public String16Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        Encoding.UTF8.GetBytes(span.Slice(0,int.Min(span.Length, Length)), AsSpan());
    }

    private fixed byte _value[Length];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Length);
}

public unsafe struct String8Utf8
{
    public const int Length = 8;

    public String8Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        span.Slice(0,int.Min(span.Length, Length)).CopyTo(AsSpan());
    }

    public String8Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        Encoding.UTF8.GetBytes(span.Slice(0,int.Min(span.Length, Length)), AsSpan());
    }

    private fixed byte _value[Length];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Length);
}