namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{
    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
}