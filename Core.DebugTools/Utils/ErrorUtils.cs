namespace Core.DebugTools.Utils;

internal static class ErrorUtils
{
    public static bool IsUserOrDataError(Exception ex) => ex switch
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
}