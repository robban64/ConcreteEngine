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
    public static int GetNullTerminateIndex(ref byte str)
    {
        var i = 0;
        while (Unsafe.Add(ref str, i) != 0) i++;
        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyByteNullTerminated(ref byte str, ref byte dest)
    {
        var len = GetNullTerminateIndex(ref str);
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
    public static unsafe int FormatChar(ref byte value, char c)
    {
        if (c <= 0x7F)
        {
            value = (byte)c;
            return 1;
        }

        if (c <= 0x7FF)
        {
            Unsafe.Add(ref value, 0) = (byte)(0xC0 | (c >> 6));
            Unsafe.Add(ref value, 1) = (byte)(0x80 | (c & 0x3F));
            return 2;
        }

        Unsafe.Add(ref value, 0) = (byte)(0xE0 | (c >> 12));
        Unsafe.Add(ref value, 1) = (byte)(0x80 | ((c >> 6) & 0x3F));
        Unsafe.Add(ref value, 2) = (byte)(0x80 | (c & 0x3F));
        return 3;
    }

    public static uint PackFormatChar(char c)
    {
        if (c <= 0x7F)
            return StringPacker.PackUtf8((byte)c, 0, 0);

        if (c <= 0x7FF)
            return StringPacker.PackUtf8((byte)(0xC0 | (c >> 6)), (byte)(0x80 | (c & 0x3F)), 0);

        return StringPacker.PackUtf8((byte)(0xE0 | (c >> 12)), (byte)(0x80 | ((c >> 6) & 0x3F)),
            (byte)(0x80 | (c & 0x3F)));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Format(int value, ref byte src, int capacity)
    {
        var negative = value < 0;
        if (!negative && capacity < 2 || negative && capacity < 3)
        {
            if (capacity > 1) Unsafe.Add(ref src, 0) = 0;
            return 0;
        }

        if (negative)
        {
            Unsafe.Add(ref src, 0) = 0x2D;
            capacity -= 1;
            src += 1;
        }

        var abs = (uint)(negative ? -value : value);
        return Format(abs, ref src, capacity) + (negative ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Format(uint value, ref byte src, int capacity)
    {
        var estimatedDigits = value < 10 ? 1 :
            value < 100 ? 2 :
            value < 1000 ? 3 :
            value < 10000 ? 4 :
            value < 100000 ? 5 :
            value < 1000000 ? 6 :
            value < 10000000 ? 7 :
            value < 100000000 ? 8 :
            value < 1000000000 ? 9 : 10;

        var digits = int.Min(estimatedDigits, capacity - 1);

        ref var cur = ref Unsafe.Add(ref src, digits);
        cur = 0; // null terminate

        for (var i = 0; i < digits; i++)
        {
            var oldValue = value;
            value /= 10;
            var mod = oldValue - value * 10;
            cur = ref Unsafe.Subtract(ref cur, 1);
            cur = (byte)('0' + mod);
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