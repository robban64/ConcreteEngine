using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Specs.Graphics;

namespace ConcreteEngine.Editor.Utils;

using StoreKind = GraphicsHandleKind;

internal static class MetricsFormatter
{
    public static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024} KB";

    public static string FormatGfxStoreMeta(in GfxStoreMeta meta)
    {
        ref readonly var m = ref meta.MetaInfo;
        return meta.Kind switch
        {
            StoreKind.Texture => FormatTexture(in m),
            StoreKind.Shader => $"{m.Value} Smpl",
            StoreKind.Mesh => $"{CountK(m.Value)}k tri",
            StoreKind.VertexBuffer or StoreKind.IndexBuffer or StoreKind.UniformBuffer => FormatBytes(m.Value),
            StoreKind.FrameBuffer => $"{FormatPixelsTier(m.Value)}×{m.ResourceId}",
            StoreKind.RenderBuffer => $"x{m.Value}",
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