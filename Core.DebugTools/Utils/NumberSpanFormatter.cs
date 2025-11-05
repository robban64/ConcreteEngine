namespace Core.DebugTools.Utils;

public readonly ref struct NumberSpanFormatter(Span<char> buffer)
{
    private readonly Span<char> _buffer = buffer;

    public ReadOnlySpan<char> Format(float value, string format = "F2")
    {
        if (!value.TryFormat(_buffer, out int charsWritten, format))
            throw new InvalidOperationException("Buffer too small for float formatting.");

        return _buffer.Slice(0, charsWritten);
    }

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
