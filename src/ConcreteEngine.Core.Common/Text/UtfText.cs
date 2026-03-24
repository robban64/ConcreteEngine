using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace ConcreteEngine.Core.Common.Text;

public static class UtfText
{
    public static bool IsAscii(ReadOnlySpan<byte> span)
    {
        foreach (var b in span)
        {
            if (b >= 0x7F) return false;
        }

        return true;
    }

    public static bool IsAscii(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (!char.IsAscii(c)) return false;
        }

        return true;
    }

    public static void SliceNullTerminate(Span<byte> byteSpan, out Span<byte> dest)
    {
        var length = byteSpan.IndexOf((byte)0);
        dest = length < 0 ? byteSpan : byteSpan.Slice(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int StrLengthNullTerminated(ref byte str)
    {
        var i = 0;
        while (Unsafe.Add(ref str, i) != 0) i++;
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyByteNullTerminated(ref byte str, ref byte dest)
    {
        int len = StrLengthNullTerminated(ref str);
        Unsafe.CopyBlockUnaligned(ref dest, ref str, (uint)len + 1);
        return len;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteCharToByteSpan(ReadOnlySpan<char> span, Span<byte> dst)
    {
        Utf8.FromUtf16(span, dst[..^1], out _, out var bytesWritten, replaceInvalidSequences: false);
        return bytesWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteByteToCharSpan(ReadOnlySpan<byte> span, Span<char> dst)
    {
        Utf8.ToUtf16(span, dst[..^1], out _, out var charsWritten, replaceInvalidSequences: false);
        return charsWritten;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int FormatChar(byte* ptr, char c)
    {
        if (c <= 0x7F)
        {
            *ptr = (byte)c;
            return 1;
        }

        if (c <= 0x7FF)
        {
            ptr[0] = (byte)(0xC0 | (c >> 6));
            ptr[1] = (byte)(0x80 | (c & 0x3F));
            return 2;
        }

        ptr[0] = (byte)(0xE0 | (c >> 12));
        ptr[1] = (byte)(0x80 | ((c >> 6) & 0x3F));
        ptr[2] = (byte)(0x80 | (c & 0x3F));
        return 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Format(int value, byte* buffer, int capacity)
    {
        var negative = value < 0;
        if (!negative && capacity < 2 || negative && capacity < 3)
        {
            if (capacity > 1) buffer[0] = 0;
            return 0;
        }

        if (negative)
        {
            buffer[0] = 0x2D;
            capacity -= 1;
            buffer += 1;
        }

        var abs = (uint)(negative ? -value : value);
        return Format(abs, buffer, capacity) + (negative ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Format(uint value, byte* buffer, in int bufSize)
    {
        var end = buffer + bufSize - 1;

        var estimatedDigits = value < 10 ? 1 :
            value < 100 ? 2 :
            value < 1000 ? 3 :
            value < 10000 ? 4 :
            value < 100000 ? 5 :
            value < 1000000 ? 6 :
            value < 10000000 ? 7 :
            value < 100000000 ? 8 :
            value < 1000000000 ? 9 : 10;

        buffer += estimatedDigits;

        if (buffer > end) buffer = end;
        *buffer = 0;

        for (var i = 0; i < estimatedDigits; i++)
        {
            var oldValue = value;
            value /= 10;
            var mod = oldValue - value * 10;
            *--buffer = (byte)('0' + mod);
        }

        return estimatedDigits;
    }
}

public static class UtfTextExtensions
{
    public static byte[] ToUtf8(this string? str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return Encoding.UTF8.GetBytes(str);
    }

    public static byte[] ToUtf8(this ReadOnlySpan<char> str)
    {
        ArgumentOutOfRangeException.ThrowIfZero(str.Length);
        var len = Encoding.UTF8.GetByteCount(str);
        ArgumentOutOfRangeException.ThrowIfZero(len, nameof(str));
        var utf = new byte[Encoding.UTF8.GetByteCount(str)];
        Encoding.UTF8.GetBytes(str, utf);
        return utf;
    }

    public static byte[][] ToUtf8ByteArrays(this ReadOnlySpan<string> strings)
    {
        var result = new byte[strings.Length][];
        for (var i = 0; i < strings.Length; i++)
            result[i] = Encoding.UTF8.GetBytes(strings[i]);
        return result;
    }
}