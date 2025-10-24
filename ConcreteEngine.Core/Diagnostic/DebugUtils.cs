namespace ConcreteEngine.Core.Diagnostic;

internal sealed class DebugUtils
{
    internal static int GetShadowSize(int size)
    {
        if (size == 1) size = 1024;
        else if (size == 2) size = 2048;
        else if (size == 4) size = 4096;
        else if (size == 8) size = 8192;

        if (size is 1024 or 2048 or 4096 or 8192)
            return size;

        return -1;
    }

}