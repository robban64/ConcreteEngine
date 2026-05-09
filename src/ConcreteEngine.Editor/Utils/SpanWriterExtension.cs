using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class SpanWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref NativeSpanWriter AppendIcon(this ref NativeSpanWriter sw, byte* icon)
    {
        var cursor = sw.Cursor;
        sw.Buffer[cursor++] = icon[0];
        if (icon[1] != 0) sw.Buffer[cursor++] = icon[1];
        if (icon[2] != 0) sw.Buffer[cursor++] = icon[2];
        sw.SetCursor(cursor);
        return ref sw;
    }
}