namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{
    public const char EndOfBuffer = '\0';
    public const string Yes = "Yes";
    public const string No = "No";
    public const string True = "True";
    public const string False = "False";
    public const string Null = "Null";

    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
    
    private static readonly byte[] MainBuffer = new byte[256];
    public static SpanWriter Writer1() => new(MainBuffer.AsSpan(0, 128));
    public static SpanWriter Writer2() => new(MainBuffer.AsSpan(128));

}