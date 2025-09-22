#region

using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal static class GfxUtilsInternal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalcMipLevels(int width, int height)
    {
        if (width <= 0 || height <= 0) return 0;
        var size = Math.Max(width, height);
        return (int)Math.Floor(Math.Log2(size)) + 1;
    }

    public static Vector2D<int> ResolveEffectiveSize(
        Vector2D<int> viewSize, Vector2 downscaleRatio, Vector2D<int>? absoluteSize)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(viewSize, default);

        var rX = downscaleRatio.X <= 0f || downscaleRatio.X > 1f ? 1f : downscaleRatio.X;
        var rY = downscaleRatio.Y <= 0f || downscaleRatio.Y > 1f ? 1f : downscaleRatio.Y;

        var baseSize = absoluteSize is null || absoluteSize.Value == default
            ? viewSize
            : absoluteSize.Value;

        var w = MathF.Max(1, MathF.Floor(baseSize.X * rX));
        var h = MathF.Max(1, MathF.Floor(baseSize.Y * rY));
        return new Vector2D<int>((int)w, (int)h);
    }

    public static int ResolveSamples(int samples) => samples <= 1 ? 1 : samples;

    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint GetTotalSize<T>(int count) where T : unmanaged => count * Unsafe.SizeOf<T>();
}