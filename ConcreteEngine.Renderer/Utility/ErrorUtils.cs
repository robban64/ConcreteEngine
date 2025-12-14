using ConcreteEngine.Graphics.Error;

namespace ConcreteEngine.Renderer.Utility;

internal static class ErrorUtils
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
}