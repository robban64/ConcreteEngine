using System.Globalization;

namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugParser
{
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
    

    internal static bool IsSafeError(Exception ex) => ex switch
    {
        OperationCanceledException => true,
        ArgumentNullException => true,
        ArgumentOutOfRangeException => true,
        ArgumentException => true,
        FormatException => true,
        InvalidCastException => true,
        KeyNotFoundException => true,
        FileNotFoundException => true,
        DirectoryNotFoundException => true,
        UnauthorizedAccessException => true,
        IOException => true,
        _ => false
    };

    internal static string ErrorMessageFor(Exception ex) => ex switch
    {
        ArgumentNullException a => $"Missing: {a.ParamName}",
        ArgumentOutOfRangeException a => $"Out of range: {a.ParamName}",
        ArgumentException a => $"Invalid argument: {a.ParamName} - {a.Message}",
        FormatException f => $"Format error: {f.Message}",
        InvalidCastException ic => $"Type error: {ic.Message}",
        KeyNotFoundException => "Key not found.",
        FileNotFoundException f => $"File not found: {f.FileName ?? ""}",
        DirectoryNotFoundException => "Directory not found.",
        UnauthorizedAccessException => "Access denied.",
        IOException io => $"I/O error: {io.Message}",
        _ => "Error."
    };
}