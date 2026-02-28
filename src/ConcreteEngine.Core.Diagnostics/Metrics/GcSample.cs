using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Metrics;

public enum GcActivity : byte
{
    None = 0,
    Minor = 1,
    Major = 2
}

public readonly struct GcSample(int gen0, int gen1, int gen2)
{
    public readonly short Gen0 = (short)gen0;
    public readonly short Gen1 = (short)gen1;
    public readonly short Gen2 = (short)gen2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GcActivity GetActivity(GcSample current, GcSample last)
    {
        int d0 = current.Gen0 - last.Gen0, d1 = current.Gen1 - last.Gen1, d2 = current.Gen2 - last.Gen2;
        if (d1 > 0 || d2 > 0) return GcActivity.Major;
        return d0 > 0 ? GcActivity.Minor : GcActivity.None;
    }
}