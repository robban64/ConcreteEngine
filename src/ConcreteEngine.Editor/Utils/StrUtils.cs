using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{
    private static readonly NumberFormatInfo NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
    public static int SliceNullTerminate(Span<byte> byteSpan, out Span<byte> dest)
    {
        var length = byteSpan.IndexOf((byte)0);
        dest = byteSpan.Slice(0, length);
        return length;
    }

    public static bool DecodeUtf8Input(ReadOnlySpan<byte> buffer, Span<char> dest, out Span<char> result)
    {
        result = Span<char>.Empty;
        if (dest.IsEmpty) return false;

        var length = dest.Length;
        var slice = length >= 0 ? buffer.Slice(0, length) : buffer;

        if (slice.IsEmpty) return false;

        int charCount = Encoding.UTF8.GetChars(slice, dest);
        result = dest.Slice(0, charCount).Trim();
        return !result.IsEmpty;
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