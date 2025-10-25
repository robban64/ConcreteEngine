namespace ConcreteEngine.Core.Diagnostic;

internal static class DebugUtils
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
}