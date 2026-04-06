using System.Globalization;

namespace ConcreteEngine.Engine.Gateway.Utils;

internal static class CommandUtils
{
    internal static int GetShadowSize(int size)
    {
        size = size switch
        {
            1 => 1024,
            2 => 2048,
            4 => 4096,
            8 => 8192,
            _ => size
        };

        if (size is 1024 or 2048 or 4096 or 8192) return size;

        return -1;
    }

    internal static int IntArg(ReadOnlySpan<char> value)
    {
        if (!int.TryParse(value, CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"Invalid int: '{value.ToString()}'");
        return result;
    }

    public static float FloatArg(ReadOnlySpan<char> value)
    {
        if (!float.TryParse(value, CultureInfo.InvariantCulture, out var result))
            throw new FormatException($"Invalid float: '{value.ToString()}'");
        return result;
    }

    public static bool BoolArg(ReadOnlySpan<char> value)
    {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        throw new FormatException($"Invalid bool: '{value.ToString()}'");
    }
}