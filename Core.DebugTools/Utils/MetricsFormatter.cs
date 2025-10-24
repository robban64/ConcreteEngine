using Core.DebugTools.Data;

namespace Core.DebugTools.Utils;

internal static class MetricsFormatter
{
    public static string FormatSpecialMetaMetric(in DebugGfxStoreMetricsRecord.SpecialMetric m) =>
        m.Kind switch
        {
            1 => FormatTexture(m),
            2 => $"{m.Value} Smpl",
            3 => $"{FormatCountK(m.Value)} tri",
            4 => FormatBytes(m.Value),
            5 => FormatBytes(m.Value),
            6 => FormatBytes(m.Value),
            7 => $"{FormatPixelsTier(m.Value)}×{m.Param0}",
            8 => $"x{m.Value}",
            _ => $"{m.Value}"
        };

    static string FormatTexture(in DebugGfxStoreMetricsRecord.SpecialMetric m)
    {
        var mip = (m.Param0 & 1) != 0;
        var samples = m.Param0 >> 1;
        string res = m.Value switch
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

    static string FormatCountK(long v)
    {
        if (v < 1000) return v.ToString();
        long k = (v + 500) / 1000;
        return $"{k}k";
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes}b";
        return $"{bytes / 1024}kb";
    }

    static string FormatPixelsTier(long pixels)
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