using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal static class GfxUtilsInternal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalcMipLevels(int width, int height, int depth = 1)
    {
        if (width <= 0 || height <= 0 || depth <= 0) return 0;
        int size = int.Max(width, int.Max(height, depth));
        return (int)float.Floor(float.Log2(size)) + 1;
    }

    public static Size2D CalcMipSize(int mipLevels, Size2D size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mipLevels);
        if (size.IsNegativeOrZero()) throw new ArgumentOutOfRangeException(nameof(size));

        int w = int.Max(1, size.Width >> mipLevels);
        int h = int.Max(1, size.Height >> mipLevels);
        return new Size2D(w, h);
    }
}