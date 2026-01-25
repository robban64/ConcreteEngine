using System.Text;

namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{
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

}