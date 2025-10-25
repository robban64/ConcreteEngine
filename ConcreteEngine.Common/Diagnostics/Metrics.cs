using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Common.Diagnostics;

// child struct
public readonly record struct MetricHeader(ushort Flags = 0, byte Kind = 0, byte State = 0)
{
    public static MetricHeader FromKind(byte kind) => new(Kind: kind);
    public static MetricHeader FromKind<T>(T kind) where T : unmanaged, Enum 
        => new(Kind: Unsafe.As<T, byte>(ref kind));
    public static MetricHeader FromFlags(ushort flags) => new(Flags: flags);
}

public readonly record struct MetricLargeHeader(
    ushort Flag1,
    ushort Flag2,
    ushort Flag3,
    byte Kind,
    byte Def0 = 0,
    byte Def1 = 0,
    byte Def2 = 0,
    byte Def3 = 0,
    byte Def4 = 0);

public readonly record struct CoreMetric<TSample>(int CoreId, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct FrameMetric<TSample>(long FrameId, long TimeStamp, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct StoreMetric<TSample>(in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct AssetMetric<TSample>(int AssetId, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct GfxResourceMetric<TSample>(int ResourceId, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct GlResourceMetric<TSample>(uint Handle, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;