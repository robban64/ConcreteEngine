using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Utils;

internal static unsafe class SpanWriterExtension
{
    extension(ref NativeSpanWriter sw)
    {
        public void EndNativeString()
        {
            var str = NativeString.From(sw.Buffer, sw.Capacity);
            str.SetLength(sw.Cursor);
            sw.SetCursor(0);
        }
        
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
        public ref NativeSpanWriter AppendIcon(uint iconData)
        {
            var cursor = sw.Cursor;
            var icon = (byte*)&iconData;
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

    extension(NativeSpanWriter sw)
    {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public  NativeView<byte> Write(int value)
        {
            var written = UtfText.Format(value, ref *sw.Buffer, sw.Capacity);
            sw.Ensure(written);
            sw.Buffer[written] = 0;
            return new NativeView<byte>(sw.Buffer, written);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public  NativeView<byte> Write(scoped ReadOnlySpan<char> value)
        {
            if (!Encoding.UTF8.TryGetBytes(value, sw.AsSpan(), out var written))
                Throwers.BufferOverflow(nameof(NativeSpanWriter), written, sw.Capacity);

            sw.Buffer[written] = 0;
            return new NativeView<byte>(sw.Buffer, written);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeView<byte> Write<T>(T value, ReadOnlySpan<char> format = default)
            where T : IUtf8SpanFormattable
        {
            if (!value.TryFormat(sw.RemainingSpan(), out var written, format, null))
                Throwers.BufferOverflow(nameof(NativeSpanWriter), written, sw.Capacity);

            sw.Buffer[written] = 0;
            return new NativeView<byte>(sw.Buffer, written);
        }

        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private NativeView<byte> ReturnWritten(int written)
        {
            if ((uint)written >= (uint)sw.Capacity) Throwers.BufferOverflow(nameof(NativeSpanWriter), written, sw.Capacity);
            sw.Buffer[written] = 0;
            return new NativeView<byte>(sw.Buffer, written);
        }
    }
}