namespace ConcreteEngine.Editor.Theme.Widgets;

internal abstract class Widget
{
    private static int _idCounter = 100_000;
    protected readonly int Id = _idCounter++;

    protected static ReadOnlySpan<byte> PlaceholderEmpty() => "Empty"u8;
    protected static ReadOnlySpan<byte> PlaceholderSelect() => "Select"u8;

    protected static readonly byte[] DefaultLabel = "##widget"u8.ToArray();
}