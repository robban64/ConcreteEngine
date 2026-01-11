using System.Runtime.CompilerServices;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Utils;

internal static class ZaStringExtension
{
    extension(ref ZaUtf8SpanWriter za)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter AppendFormat0(int value)
        {
            if (value < 10) za.Append(0);
            za.Append(value);
            return ref za;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter EndOfBuffer()
        {
            za.RemainingSpan[0] = 0;
            return ref za;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter AppendEnd(int value)
        {
            za.Append(value);
            za.RemainingSpan[0] = 0;
            return ref za;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter AppendEnd(long value)
        {
            za.Append(value);
            za.RemainingSpan[0] = 0;
            return ref za;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter AppendEnd(string text)
        {
            za.Append(text);
            za.RemainingSpan[0] = 0;
            return ref za;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ZaUtf8SpanWriter AppendEnd(ReadOnlySpan<byte> text)
        {
            za.Append(text);
            za.RemainingSpan[0] = 0;
            return ref za;
        }

    }

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