namespace ConcreteEngine.Core.Common.Collections;

public static class SpanExtensions
{
    public static Span<byte> TrimWhitespace(this Span<byte> span)
    {
        int start = 0;
        int end = span.Length - 1;

        while (start <= end && span[start] == (byte)' ')
            start++;

        while (end >= start && span[end] == (byte)' ')
            end--;

        return span.Slice(start, end - start + 1);
    }

    public static bool ContainsIgnoreCase(this ReadOnlySpan<string> span, ReadOnlySpan<char> value)
    {
        foreach (var s in span)
        {
            if (value.Equals(s, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

}