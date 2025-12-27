using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Engine.Utils;

internal sealed class ErrorUtils
{
    public static bool IsGfxError(Exception ex) => ex is GraphicsException;

    public static bool IsInvalidOpError(Exception ex) => ex is InvalidOperationException;

    public static bool IsUserOrDataError(Exception ex) =>
        ex switch
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

    public static string ErrorMessageFor(Exception ex) =>
        ex switch
        {
            GraphicsException gfx => $"Gfx error: {gfx.Message}",
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