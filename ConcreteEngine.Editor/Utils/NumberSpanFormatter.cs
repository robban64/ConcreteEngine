namespace ConcreteEngine.Editor.Utils;

public readonly ref struct NumberSpanFormatter(Span<char> buffer)
{
    private readonly Span<char> _buffer = buffer;

    public ReadOnlySpan<char> Format(float value, string format = "F2", int pad = 0)
    {
        if (!value.TryFormat(_buffer, out int charsWritten, format))
            throw new InvalidOperationException("Buffer too small for float formatting.");

        if (pad <= charsWritten || pad == 0)
            return _buffer.Slice(0, charsWritten);

        return SliceAndPad(charsWritten, pad);
    }

    private ReadOnlySpan<char> SliceAndPad(int charsWritten, int pad)
    {
        int totalWidth = int.Min(_buffer.Length, pad);
        int padding = totalWidth - charsWritten;

        for (int i = charsWritten - 1; i >= 0; i--) _buffer[i + padding] = _buffer[i];
        for (int i = 0; i < padding; i++) _buffer[i] = ' ';

        return _buffer.Slice(0, totalWidth);
    }
    
    
    public ReadOnlySpan<char> Format(double value, string format = "F2", int pad = 0)
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