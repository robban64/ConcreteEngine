using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal abstract class Widget
{
    private const int BufferLen = 256;
    private static int _idCounter = 100_000;

    private static readonly byte[] MainBuffer = new byte[BufferLen];
    public static SpanWriter GetWriter1() => new(MainBuffer.AsSpan(0, 128));
    public static SpanWriter GetWriter2() => new(MainBuffer.AsSpan(128));

    protected readonly int Id = _idCounter++;
}