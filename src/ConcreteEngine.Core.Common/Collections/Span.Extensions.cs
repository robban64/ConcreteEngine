using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Collections;

public static class SpanExtensions
{
    public static ReadOnlySpan<char> Truncate(this string str, int length) =>
        str.AsSpan(0, int.Min(length, str.Length));

    public static ReadOnlySpan<T> Truncate<T>(this ReadOnlySpan<T> span, int length) =>
        span.Slice(0, int.Min(length, span.Length));

    public static bool ContainsCharSpan(this ReadOnlySpan<string> span, ReadOnlySpan<char> value,
        StringComparison comparison = StringComparison.Ordinal)
    {
        foreach (var s in span)
        {
            if (value.Equals(s, comparison)) return true;
        }

        return false;
    }

    extension(Span<byte> span)
    {
        public Span<byte> SliceNullTerminate()
        {
            UtfText.SliceNullTerminate(span, out var byteSpan);
            return byteSpan;
        }

        public Span<byte> TrimWhitespace()
        {
            int start = 0, end = span.Length - 1;

            while (start <= end && span[start] == (byte)' ') start++;

            while (end >= start && span[end] == (byte)' ') end--;

            return span.Slice(start, end - start + 1);
        }

        public Span<byte> ToLowerAscii()
        {
            for (var i = 0; i < span.Length; i++)
            {
                var b = span[i];
                if (b >= (byte)'A' && b <= (byte)'Z')
                    span[i] = (byte)(b + 32);
            }

            return span;
        }

        public Span<byte> ToUpperAscii()
        {
            for (var i = 0; i < span.Length; i++)
            {
                var b = span[i];
                if (b >= (byte)'a' && b <= (byte)'z')
                    span[i] = (byte)(b - 32);
            }

            return span;
        }
    }
}