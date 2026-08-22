using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Text;


[InterpolatedStringHandler]
public ref struct NativeSpanWriterHandler(int literalLength, int formattedCount, NativeSpanWriter writer)
{
    private NativeSpanWriter _writer = writer;

    public void AppendLiteral(string s) => _writer.Append(s);

    public void AppendLiteral(ReadOnlySpan<char> s) => _writer.Append(s);
    public void AppendLiteral(ReadOnlySpan<byte> s) => _writer.Append(s);

    public void AppendFormatted<T>(T t) where T : IUtf8SpanFormattable => _writer.Append(t);

    public void AppendFormatted<T>(T t, string? format) where T : IUtf8SpanFormattable => _writer.Append(t, format);

    public void AppendFormatted(string? s)
    {
        if (s is not null) _writer.Append(s);
    }

    public void AppendFormatted<T>(T value, int alignment) where T : IUtf8SpanFormattable
    {
        Span<byte> tmp = stackalloc byte[64];
        if (value.TryFormat(tmp, out int written, default, null))
        {
            int pad = Math.Abs(alignment) - written;
            if (alignment > 0) _writer.PadRight(pad);
            _writer.Append(tmp[..written]);
            if (alignment < 0) _writer.PadRight(pad);
        }
        else
        {
            _writer.Append(value);
        }
    }

    public void AppendFormatted<T>(T value, int alignment, string? format) where T : IUtf8SpanFormattable
    {
        Span<byte> tmp = stackalloc byte[64];
        if (value.TryFormat(tmp, out int written, format, null))
        {
            int pad = Math.Abs(alignment) - written;
            if (alignment > 0) _writer.PadRight(pad);
            _writer.Append(tmp[..written]);
            if (alignment < 0) _writer.PadRight(pad);
        }
        else
        {
            _writer.Append(value);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> s) => _writer.Append(s);
    public void AppendFormatted(ReadOnlySpan<byte> s) => _writer.Append(s);

    public void AppendFormatted(object? value)
    {
        if (value is not null) _writer.Append(value.ToString()!);
    }

    public void AppendFormatted(object? value, string? format)
    {
        if (value is IFormattable f) _writer.Append(f.ToString(format, null)!);
        else if (value is not null) _writer.Append(value.ToString()!);
    }
}