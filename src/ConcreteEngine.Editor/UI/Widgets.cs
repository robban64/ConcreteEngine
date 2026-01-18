using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.UI;

internal static class Widgets
{
    private static int _idCounter = 100_000;
    public static int NextId() => _idCounter++;

    private static readonly byte[] MainBuffer = new byte[256];

    public static SpanWriter GetWriter1() => new(MainBuffer.AsSpan(128));
    public static SpanWriter GetWriter2() => new(MainBuffer.AsSpan(128));
}