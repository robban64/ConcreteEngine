namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{

    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
    
    private static readonly byte[] MainBuffer = new byte[256];
    public static SpanWriter WidgetSw1() => new(MainBuffer.AsSpan(0, 128));
    public static SpanWriter WidgetSw2() => new(MainBuffer.AsSpan(128));

}