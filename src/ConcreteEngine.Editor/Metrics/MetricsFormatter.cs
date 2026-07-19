using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Editor.Metrics;

internal static class MetricsFormatter
{
    public static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024} KB";

    public static string FormatGfxStoreMeta(in GfxStoreMeta meta)
    {
        ref readonly var m = ref meta.MetaInfo;
        return meta.Kind switch
        {
            GraphicsKind.Texture => FormatTexture(in m),
            GraphicsKind.Shader => $"{m.Value} Smpl",
            GraphicsKind.Mesh => $"{CountK(m.Value)}k tri",
            GraphicsKind.VertexBuffer or GraphicsKind.IndexBuffer or GraphicsKind.UniformBuffer => FormatBytes(m.Value),
            GraphicsKind.FrameBuffer => $"{FormatPixelsTier(m.Value)}×{m.ResourceId}",
            GraphicsKind.RenderBuffer => $"x{m.Value}",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static string FormatTexture(in GfxMetaInfo m)
    {
        var flags = (ushort)m.Param;

        var mip = (flags & 1) != 0;
        var samples = flags >> 1;
        var res = m.Value switch
        {
            < 1024 => "512",
            < 2048 => "1k",
            < 4096 => "2k",
            < 8192 => "4k",
            _ => "8k"
        };
        var s = res;
        if (samples > 1) s += $"x{samples}";
        if (mip) s += " M";
        return s;
    }

    private static long CountK(long v)
    {
        if (v < 1000) return v;
        return (v + 500) / 1000;
    }

    private static string FormatPixelsTier(long pixels)
    {
        var f = (long)Math.Round(Math.Sqrt(pixels));
        return f switch
        {
            < 1024 => "512",
            < 2048 => "1k",
            < 4096 => "2k",
            < 8192 => "4k",
            _ => "8k"
        };
    }
}