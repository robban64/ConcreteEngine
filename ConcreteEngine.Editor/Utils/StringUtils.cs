namespace ConcreteEngine.Editor.Utils;

internal static class StringUtils
{
    public static readonly char[] CharBuffer8 = new char[8];
    public static readonly char[] CharBuffer16 = new char[16];

    public static string BoolToYesNo(bool value, bool longVersion = false)
    {
        if (longVersion) return value ? "Yes" : "No";
        return value ? "Y" : "N";
    }
}