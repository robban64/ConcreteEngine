namespace ConcreteEngine.Graphics.Error;

internal static class ErrorUtils
{
    public static string MakeGlErrorMessage(string stage, string? debugName, string glLog)
    {
        var lines = glLog.Split('\n');
        var first = "";
        foreach (var l in lines)
        {
            var ln = l.Trim();
            if (ln.Length == 0) continue;
            first = ln;
            break;
        }

        if (string.IsNullOrEmpty(first)) first = "GL reported an error.";

        if (!string.IsNullOrEmpty(debugName)) return $"{stage} failed for \"{debugName}\": {first}";

        return $"{stage} failed: {first}";
    }

    internal static bool IsSafeError(Exception ex, bool catchGfxError = true) =>
        ex switch
        {
            GraphicsException => catchGfxError,
            ArgumentNullException => true,
            ArgumentOutOfRangeException => true,
            ArgumentException => true,
            FormatException => true,
            InvalidCastException => true,
            KeyNotFoundException => true,
            _ => false
        };
}