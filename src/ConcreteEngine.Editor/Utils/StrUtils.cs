using System.Runtime.CompilerServices;

namespace ConcreteEngine.Editor.Utils;

internal static class StrUtils
{
    public const char EndOfBuffer = '\0';
    public const string Yes = "Yes";
    public const string No = "No";
    public const string True = "True";
    public const string False = "False";
    public const string Null = "Null";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> BoolToYesNoShort(bool value) => value ? "Y"u8 : "N"u8;
}