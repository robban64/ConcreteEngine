using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Text;

file static class Utils
{
    public static void CopyByteSpan(ReadOnlySpan<byte> span, Span<byte> dst)
    {
        if (span.IsEmpty) return;
        var len = int.Min(span.Length, dst.Length);
        span.Slice(0, len).CopyTo(dst);
        dst[len] = 0;
    }
}

public unsafe struct String64Utf8
{
    public const int Capacity = 64;

    private fixed byte _value[Capacity];

    public String64Utf8(ReadOnlySpan<byte> span)
    {
        Utils.CopyByteSpan(span, AsSpan());
    }

    public String64Utf8(ReadOnlySpan<char> span)
    {
        UtfText.WriteCharToByteSpan(span, AsSpan());
    }

    public static implicit operator String64Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String64Utf8(ReadOnlySpan<byte> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
}

public unsafe struct String32Utf8
{
    public const int Capacity = 32;
    private fixed byte _value[Capacity];

    public String32Utf8(ReadOnlySpan<byte> span)
    {
        Utils.CopyByteSpan(span, AsSpan());
    }

    public String32Utf8(ReadOnlySpan<char> span)
    {
        UtfText.WriteCharToByteSpan(span, AsSpan());
    }

    public static implicit operator String32Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String32Utf8(ReadOnlySpan<byte> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
}

public unsafe struct String16Utf8
{
    public const int Capacity = 16;
    private fixed byte _value[Capacity];

    public String16Utf8(ReadOnlySpan<byte> span)
    {
        Utils.CopyByteSpan(span, AsSpan());
    }

    public String16Utf8(ReadOnlySpan<char> span)
    {
        UtfText.WriteCharToByteSpan(span, AsSpan());
    }

    public static implicit operator String16Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String16Utf8(ReadOnlySpan<byte> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
}

public unsafe struct String8Utf8
{
    public const int Capacity = 8;

    private fixed byte _value[Capacity];

    public String8Utf8(ReadOnlySpan<byte> span)
    {
        Utils.CopyByteSpan(span, AsSpan());
    }

    public String8Utf8(ReadOnlySpan<char> span)
    {
        UtfText.WriteCharToByteSpan(span, AsSpan());
    }

    public static implicit operator String8Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String8Utf8(ReadOnlySpan<byte> value) => new(value);

    public bool IsEmpty => _value[0] == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);
}