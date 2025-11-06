#region

using ConcreteEngine.Common.Diagnostics;

#endregion

namespace ConcreteEngine.Editor.Utils;

internal static class MetricsFormatter
{
    public static string FormatMb(long bytes) => $"{bytes / 1024 / 1024} MB";
    public static string Format(float value) => value.ToString("0.00");

    public static string FormatBytes(long bytes) => bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024} KB";

    public static string FormatSpecialMetaMetric(in GfxResourceMetric<ValueSample> m)
    {
        var sample = m.Sample;
        return m.Header.Kind switch
        {
            1 => FormatTexture(in m),
            2 => $"{sample.Value} Smpl",
            3 => $"{FormatCountK(sample.Value)} tri",
            4 or 5 or 6 => FormatBytes(sample.Value),
            7 => $"{FormatPixelsTier(sample.Value)}×{sample.Param0}",
            8 => $"x{sample.Value}",
            _ => $"{sample.Value}"
        };
    }

    private static string FormatTexture(in GfxResourceMetric<ValueSample> m)
    {
        var header = m.Header;

        var mip = (header.Flags & 1) != 0;
        var samples = header.Flags >> 1;
        string res = m.Sample.Value switch
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

    private static string FormatCountK(long v)
    {
        if (v < 1000) return v.ToString();
        long k = (v + 500) / 1000;
        return $"{k}k";
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