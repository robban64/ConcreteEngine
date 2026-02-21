using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ConcreteEngine.Core.Common.Text;

file static class StringUtfUtils
{
    public static int FromByteSpan(ReadOnlySpan<byte> span, Span<byte> dst)
    {
        if (span.IsEmpty) return 0;
        var len = int.Min(span.Length, dst.Length - 1);
        span.Slice(0, len).CopyTo(dst);
        dst[len] = 0;
        return len;
    }

    public static int FromCharSpan(ReadOnlySpan<char> span, Span<byte> dst)
    {
        if (span.IsEmpty) return 0;
        var len = int.Min(span.Length, dst.Length - 1);
        var written = Encoding.UTF8.GetBytes(span.Slice(0, len), dst);
        dst[len] = 0;
        return written;
    }
}

public unsafe struct String64Utf8
{
    public const int Capacity = 64;

    private fixed byte _value[Capacity];

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


    public readonly bool IsEmpty => _value[0] == 0;
    public readonly int Length => _value[Capacity - 1] > 0 ? _value[Capacity - 1] : Capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringSpan()
    {
        if (Length == 0) return ReadOnlySpan<byte>.Empty;
        return AsSpan().Slice(0, Length);
    }
}

public unsafe struct String32Utf8
{
    public const int Capacity = 32;
    private fixed byte _value[Capacity];

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

    public readonly bool IsEmpty => _value[0] == 0;
    public readonly int Length => _value[Capacity - 1] > 0 ? _value[Capacity - 1] : Capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringSpan()
    {
        if (Length == 0) return ReadOnlySpan<byte>.Empty;
        return AsSpan().Slice(0, Length);
    }
}

public unsafe struct String16Utf8
{
    public const int Capacity = 16;
    private fixed byte _value[Capacity];

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

    public readonly int Length => _value[Capacity - 1] > 0 ? _value[Capacity - 1] : Capacity;
    public readonly bool IsEmpty => _value[0] == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringSpan()
    {
        if (Length == 0) return ReadOnlySpan<byte>.Empty;
        return AsSpan().Slice(0, Length);
    }
}

public unsafe struct String8Utf8
{
    public const int Capacity = 8;

    private fixed byte _value[Capacity];

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

    public readonly int Length => _value[Capacity - 1] > 0 ? _value[Capacity - 1] : Capacity;

    public readonly bool IsEmpty => _value[0] == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], Capacity);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringSpan()
    {
        if (Length == 0) return ReadOnlySpan<byte>.Empty;
        return AsSpan().Slice(0, Length);
    }
}