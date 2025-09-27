#region

using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal static class GfxUtilsInternal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalcMipLevels(int width, int height, int depth = 1)
    {
        if (width <= 0 || height <= 0 || depth <= 0) return 0;
        int size = Math.Max(width, Math.Max(height, depth));
        return (int)Math.Floor(Math.Log2(size)) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetTotalSize<T>(int count) where T : unmanaged => count * Unsafe.SizeOf<T>();
}