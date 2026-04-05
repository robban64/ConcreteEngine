using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public static class SpanWriterExtensions
{
    extension(ref UnsafeSpanWriter sw)
    {
        public unsafe NativeViewPtr<byte> EndViewPtr()
        {
            var cursor = sw.Cursor;
            return new NativeViewPtr<byte>(sw.End(), cursor);
        }
    }

    extension(ref SpanWriter sw)
    {
        public ref SpanWriter Start(ReadOnlySpan<byte> value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref SpanWriter Start(ReadOnlySpan<char> value)
        {
            sw.Clear();
            return ref sw.Append(value);
        }

        public ref SpanWriter Start<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            sw.Clear();
            return ref sw.Append(value, format);
        }


        public ref SpanWriter AppendPadRight(ReadOnlySpan<char> value, int pad)
        {
            sw.Append(value);
            if (pad == 0) return ref sw;
            var padLeft = int.Max(0, pad - value.Length);
            return ref sw.PadRight(padLeft);
        }

        public ref SpanWriter AppendPadRight<T>(T value, int pad, ReadOnlySpan<char> format = default)
            where T : ISpanFormattable
        {
            var start = sw.Cursor;
            sw.Append(value, format);
            if (pad == 0) return ref sw;
            var written = sw.Cursor - start;
            var padLeft = int.Max(0, pad - written);
            return ref sw.PadRight(padLeft);
        }
    }
}