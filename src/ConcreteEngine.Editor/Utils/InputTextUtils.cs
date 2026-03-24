using System.Text;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static class InputTextUtils
{
    public static Span<char> GetSearchString(Span<byte> src, Span<char> dst, out ulong key, out ulong mask)
    {
        key = 0;
        mask = 0;

        UtfText.SliceNullTerminate(src, out var byteSpan);
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return Span<char>.Empty;

        key = StringPacker.PackAscii(byteSpan, true);
        mask = StringPacker.GetMaskUtf8(byteSpan.Length);

        Encoding.UTF8.GetChars(byteSpan, dst);
        return dst.Trim();
    }
}