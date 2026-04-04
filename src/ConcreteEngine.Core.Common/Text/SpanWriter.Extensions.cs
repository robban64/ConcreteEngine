namespace ConcreteEngine.Core.Common.Text;

public static class SpanWriterExtensions
{
    extension(ref SpanWriter writer)
    {
        public ref SpanWriter Start(ReadOnlySpan<byte> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        public ref SpanWriter Start(ReadOnlySpan<char> value)
        {
            writer.Clear();
            return ref writer.Append(value);
        }

        public ref SpanWriter Start<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            writer.Clear();
            return ref writer.Append(value, format);
        }


        public ref SpanWriter AppendPadRight(ReadOnlySpan<char> value, int pad)
        {
            writer.Append(value);
            if (pad == 0) return ref writer;
            var padLeft = int.Max(0, pad - value.Length);
            return ref writer.PadRight(padLeft);
        }

        public ref SpanWriter AppendPadRight<T>(T value, int pad, ReadOnlySpan<char> format = default)
            where T : ISpanFormattable
        {
            var start = writer.Cursor;
            writer.Append(value, format);
            if (pad == 0) return ref writer;
            var written = writer.Cursor - start;
            var padLeft = int.Max(0, pad - written);
            return ref writer.PadRight(padLeft);
        }
    }
}