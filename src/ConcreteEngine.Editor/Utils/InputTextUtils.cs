using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static class InputTextUtils
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Span<char> GetSearchString(Span<byte> byteSpan, Span<char> dst, out ulong key, out ulong mask)
    {
        key = 0;
        mask = 0;

        byteSpan = byteSpan.SliceNullTerminate();
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return Span<char>.Empty;

        key = StringPacker.PackAscii(byteSpan, true);
        mask = StringPacker.GetMaskUtf8(byteSpan.Length);

        Encoding.UTF8.GetChars(byteSpan, dst);
        return dst.Trim();
    }
}