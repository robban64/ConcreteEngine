namespace Core.DebugTools.Utils;

internal ref struct NumberSpanFormatter(Span<char> buffer)
{
    private Span<char> _buffer = buffer;

    public ReadOnlySpan<char> Format(int value)
    {
        if (!value.TryFormat(_buffer, out int charsWritten))
            throw new InvalidOperationException("Buffer too small for int formatting.");

        return _buffer.Slice(0, charsWritten);
    }
    
    public ReadOnlySpan<char> Format(long value)
    {
        if (!value.TryFormat(_buffer, out int charsWritten))
            throw new InvalidOperationException("Buffer too small for long formatting.");

        return _buffer.Slice(0, charsWritten);
    }

}
