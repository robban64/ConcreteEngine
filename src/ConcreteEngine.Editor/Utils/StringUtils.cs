using System.Runtime.CompilerServices;

namespace ConcreteEngine.Editor.Utils;

internal static class StringUtils
{
    public static readonly char[] CharBuffer8 = new char[8];
    public static readonly char[] CharBuffer16 = new char[16];

    public const string Yes = "Yes";
    public const string No = "No";
    public const string True = "True";
    public const string False = "False";
    public const string Null = "Null";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BoolToYesNoShort(bool value) => value ? "Y" : "N";
}