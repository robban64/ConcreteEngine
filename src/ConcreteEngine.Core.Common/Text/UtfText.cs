using System.Runtime.CompilerServices;
using System.Text;

namespace ConcreteEngine.Core.Common.Text;

public static class UtfText
{
    public static int SliceNullTerminate(Span<byte> byteSpan, out Span<byte> dest)
    {
        var length = byteSpan.IndexOf((byte)0);
        dest = byteSpan.Slice(0, length);
        return length;
    }

    public static byte[][] ToUtf8ByteArrays(ReadOnlySpan<string> strings)
    {
        var result = new byte[strings.Length][];
        for (var i = 0; i < strings.Length; i++)
            result[i] = Encoding.UTF8.GetBytes(strings[i]);
        return result;
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

        var estimatedDigits = (value < 10) ? 1 :
            (value < 100) ? 2 :
            (value < 1000) ? 3 :
            (value < 10000) ? 4 :
            (value < 100000) ? 5 :
            (value < 1000000) ? 6 :
            (value < 10000000) ? 7 :
            (value < 100000000) ? 8 :
            (value < 1000000000) ? 9 : 10;

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
}