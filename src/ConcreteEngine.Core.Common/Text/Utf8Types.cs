using System.Runtime.InteropServices;
using System.Text;

namespace ConcreteEngine.Core.Common.Text;

file static class StringUtfUtils
{
    public static void FromByteSpan(ReadOnlySpan<byte> span, Span<byte> dst)
    {
        if (span.IsEmpty) return;
        var len = int.Min(span.Length, dst.Length - 1);
        span.Slice(0, len).CopyTo(dst);
        dst[len] = 0;
    }
    
    public static void FromCharSpan(ReadOnlySpan<char> span, Span<byte> dst)
    {
        if (span.IsEmpty) return;
        var len = int.Min(span.Length, dst.Length - 1);
        Encoding.UTF8.GetBytes(span.Slice(0, len), dst);
        dst[len] = 0;
    }

}

public unsafe struct String64Utf8
{
    public const int Capacity = 64;

    public String64Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromByteSpan(span, AsSpan());
    }

    public String64Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromCharSpan(span, AsSpan());
    }

    private fixed byte _value[Capacity];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
    public ReadOnlySpan<byte> GetStringSpan()
    {
        var span = AsSpan();
        var len = span.IndexOf((byte)0);
        if (len < 0) return ReadOnlySpan<byte>.Empty;
        return span.Slice(0, len);
    }

}

public unsafe struct String32Utf8
{
    public const int Capacity = 32;

    public String32Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromByteSpan(span, AsSpan());
    }

    public String32Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromCharSpan(span, AsSpan());
    }

    private fixed byte _value[Capacity];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
    public ReadOnlySpan<byte> GetStringSpan()
    {
        var span = AsSpan();
        var len = span.IndexOf((byte)0);
        if (len < 0) return ReadOnlySpan<byte>.Empty;
        return span.Slice(0, len);
    }

}

public unsafe struct String16Utf8
{
    public const int Capacity = 16;

    public String16Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromByteSpan(span, AsSpan());
    }

    public String16Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromCharSpan(span, AsSpan());
    }

    private fixed byte _value[Capacity];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
    
    public ReadOnlySpan<byte> GetStringSpan()
    {
        var span = AsSpan();
        var len = span.IndexOf((byte)0);
        if (len < 0) return ReadOnlySpan<byte>.Empty;
        return span.Slice(0, len);
    }

}

public unsafe struct String8Utf8
{
    public const int Capacity = 8;

    public String8Utf8(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromByteSpan(span, AsSpan());
    }

    public String8Utf8(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty) return;
        StringUtfUtils.FromCharSpan(span, AsSpan());
    }

    private fixed byte _value[Capacity];
    public readonly bool IsEmpty => _value[0] == 0;
    public ref byte GetRef() => ref _value[0];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);

    public ReadOnlySpan<byte> GetStringSpan()
    {
        var span = AsSpan();
        var len = span.IndexOf((byte)0);
        if (len < 0) return ReadOnlySpan<byte>.Empty;
        return span.Slice(0, len);
    }
}