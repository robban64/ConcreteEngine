using System.Runtime.CompilerServices;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Utils;

internal static class ZaStringExtension
{

    extension(ref ZaSpanStringBuilder za)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaSpanStringBuilder PadLeft(ReadOnlySpan<char> text, int totalWidth, char padChar = ' ')
        {
            int toPad = totalWidth - text.Length;
            if (toPad > 0) za.AppendRepeat(padChar, toPad);
            za.Append(text);
            return ref za;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaSpanStringBuilder PadRight(ReadOnlySpan<char> text, int space, char padChar = ' ')
        {
            za.Append(text);
            int toPad = space - text.Length;
            if (toPad > 0) za.AppendRepeat(padChar, toPad);
            return ref za;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaSpanStringBuilder PadRight(ReadOnlySpan<char> t1, ReadOnlySpan<char> t2, int space,
            char padChar = ' ')
        {
            za.Append(t1).Append(t2);
            int toPad = space - (t1.Length + t2.Length);
            if (toPad > 0) za.AppendRepeat(padChar, toPad);
            return ref za;
        }
    }
}