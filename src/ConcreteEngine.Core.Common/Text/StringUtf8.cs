using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct String32Utf8
{
    public const int Capacity = 32;
    public const int TextLength = 30;
    private fixed byte _value[Capacity];

    public String32Utf8(ReadOnlySpan<byte> span)
    {
        var src = span.Truncate(TextLength);
        src.CopyTo(AsSpan());
        _value[src.Length] = 0;
        _value[TextLength + 1] = (byte)span.Length;
    }

    public String32Utf8(ReadOnlySpan<char> span)
    {
        int written = Encoding.UTF8.GetBytes(span.Truncate(TextLength), AsSpan());
        _value[written] = 0;
        _value[TextLength + 1] = (byte)written;
    }
    
    public readonly int Count => _value[TextLength + 1];

    public static implicit operator String32Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String32Utf8(ReadOnlySpan<byte> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsTextSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _value[0]), Count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], TextLength);
}

public unsafe struct String16Utf8
{
    public const int Capacity = 16;
    public const int TextLength = 14;
    
    private fixed byte _value[Capacity];

    public String16Utf8(ReadOnlySpan<byte> span)
    {
        var src = span.Truncate(TextLength);
        src.CopyTo(AsSpan());
        _value[src.Length] = 0;
        _value[TextLength + 1] = (byte)span.Length;
    }

    public String16Utf8(ReadOnlySpan<char> span)
    {
        int written = Encoding.UTF8.GetBytes(span.Truncate(TextLength), AsSpan());
        _value[written] = 0;
        _value[TextLength + 1] = (byte)written;
    }

    public readonly int Count => _value[TextLength + 1];

    public static implicit operator String16Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String16Utf8(ReadOnlySpan<byte> value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> GetTextSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _value[0]), Count);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], TextLength);
}

public unsafe struct String8Utf8
{
    public const int Capacity = 8;
    public const int TextLength = 7;

    private fixed byte _value[Capacity];


    public String8Utf8(ReadOnlySpan<byte> span)
    {
        var src = span.Truncate(TextLength);
        src.CopyTo(AsSpan());
        _value[src.Length] = 0;
    }

    public String8Utf8(ReadOnlySpan<char> span)
    {
        int written = Encoding.UTF8.GetBytes(span.Truncate(TextLength), AsSpan());
        _value[written] = 0;
    }


    public static implicit operator String8Utf8(ReadOnlySpan<char> value) => new(value);
    public static implicit operator String8Utf8(ReadOnlySpan<byte> value) => new(value);

    public readonly bool IsEmpty => _value[0] == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref byte GetRef(int i = 0) => ref _value[i];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value[0], TextLength);
}