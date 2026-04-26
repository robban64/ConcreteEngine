namespace ConcreteEngine.Editor.Theme;
/*
internal static unsafe class TextMap
{
    private const int Stride = 16;

    private static NativeArray<byte> _textBuffer;
    private static NativeView<byte> _logLevelView;
    private static NativeView<byte> _logScopeView;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeView<byte> GetLogLevelText(LogLevel i) => _logLevelView.Slice((int)i * Stride, Stride);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeView<byte> GetLogScopeText(LogScope i) => _logScopeView.Slice((int)i * Stride, Stride);


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Allocate()
    {
        if (!_textBuffer.IsNull) throw new InvalidOperationException("Already allocated");

        int levelCount = EnumCache<LogLevel>.Count;
        int scopeCount = EnumCache<LogScope>.Count;

        _textBuffer = NativeArray.Allocate<byte>((levelCount + scopeCount) * Stride, true);

        _logLevelView = _textBuffer.Slice(0, Stride * levelCount);
        _logScopeView = _textBuffer.Slice(_logLevelView.Length, Stride * scopeCount);


        for (int i = 0; i < levelCount; i++)
        {
            var value = (LogLevel)i;
            _logLevelView.Slice(i * Stride, Stride).Writer().Write(value.ToLogText());
        }
        for (int i = 0; i < scopeCount; i++)
        {
            var value = (LogScope)i;
            _logScopeView.Slice(i * Stride, Stride).Writer().Write(value.ToLogText());
        }
    }

    public static void Dispose()
    {
        _textBuffer.Dispose();
    }

}*/