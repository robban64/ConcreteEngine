using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx.Internal;

internal static class GfxUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalcMipLevels(uint width, uint height)
    {
        uint size = Math.Max(width, height);
        return (uint)Math.Floor(Math.Log2(size)) + 1;
    }

    public static Vector2D<int> ResolveEffectiveSize(
        Vector2D<int> viewSize, Vector2 downscaleRatio, Vector2D<int>? absoluteSize)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(viewSize, default);

        var rX = (downscaleRatio.X <= 0f || downscaleRatio.X > 1f) ? 1f : downscaleRatio.X;
        var rY = (downscaleRatio.Y <= 0f || downscaleRatio.Y > 1f) ? 1f : downscaleRatio.Y;

        var baseSize = (absoluteSize is null || absoluteSize.Value == default)
            ? viewSize
            : absoluteSize.Value;

        var w = MathF.Max(1, MathF.Floor(baseSize.X * rX));
        var h = MathF.Max(1, MathF.Floor(baseSize.Y * rY));
        return new Vector2D<int>((int)w, (int)h);
    }

    public static int ResolveSamples(int samples) => samples <= 1 ? 1 : samples;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (uint Count, uint Stride, nuint Size) GetElementInfo<T>(int count)
        where T : unmanaged
    {
        uint stride = (uint)Unsafe.SizeOf<T>();
        return ((uint)count, stride, (nuint)(count * stride));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint GetTotalSize<T>(int count) where T : unmanaged => (nuint)(count * (uint)Unsafe.SizeOf<T>());
}