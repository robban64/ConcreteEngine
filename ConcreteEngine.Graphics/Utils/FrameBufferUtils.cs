using System.Numerics;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Utils;


internal static class FrameBufferUtils
{
    public static void GetMetaResizeCopy(in FrameBufferMeta m, Vector2D<int> size, out FrameBufferMeta meta)
    {
        meta = new FrameBufferMeta(m.ColTexId, m.RboTexId, m.RboDepthId, m.TexturePreset, m.SizeRatio, size,
            m.DepthStencilBuffer,
            m.Msaa, m.Samples);
    }
    
    public static Vector2D<int> ResolveEffectiveSize(
        Vector2D<int> viewSize, Vector2 downscaleRatio, Vector2D<int>? absoluteSize)
    {
        var rX = (downscaleRatio.X <= 0f || downscaleRatio.X > 1f) ? 1f : downscaleRatio.X;
        var rY = (downscaleRatio.Y <= 0f || downscaleRatio.Y > 1f) ? 1f : downscaleRatio.Y;

        var baseSize = (absoluteSize is null || absoluteSize.Value == default)
            ? viewSize : absoluteSize.Value;

        var w = MathF.Max(1, MathF.Floor(baseSize.X * rX));
        var h = MathF.Max(1, MathF.Floor(baseSize.Y * rY));
        return new Vector2D<int>((int)w, (int)h);
    }

    public static int ResolveSamples(int samples) => samples <= 1 ? 1 : samples;
}