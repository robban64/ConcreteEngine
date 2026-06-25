using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class SpanWriterExtension
{
    extension(ref NativeSpanWriter sw)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref NativeSpanWriter AppendImGuiId(int id)
        {
            var cursor = sw.Cursor;
            sw.Buffer[cursor++] = 0x23; // #
            sw.Buffer[cursor++] = 0x23; // #
            cursor += UtfText.Format(id, ref *(sw.Buffer + cursor), sw.Capacity - cursor);
            sw.SetCursor(cursor);
            return ref sw;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref NativeSpanWriter AppendIcon(byte* icon)
        {
            var cursor = sw.Cursor;
            if (icon[2] != 0)
            {
                sw.Buffer[cursor++] = icon[0];
                sw.Buffer[cursor++] = icon[1];
                sw.Buffer[cursor++] = icon[2];
            }
            else if (icon[1] != 0)
            {
                sw.Buffer[cursor++] = icon[0];
                sw.Buffer[cursor++] = icon[1];
            }

            sw.SetCursor(cursor);
            return ref sw;
        }
    }
}