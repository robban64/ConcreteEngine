using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Common.Diagnostics;

// child struct
public readonly record struct MetricHeader(ushort Flags = 0, byte Kind = 0, byte State = 0);

public readonly record struct MetricLargeHeader(
    ushort Flag1,
    ushort Flag2,
    ushort Flag3,
    byte Kind,
    byte Def0=0,
    byte Def1=0,
    byte Def2=0,
    byte Def3=0,
    byte Def4=0);

// composite structs (app structs)
public readonly record struct AssetMetric<TParam>(int AssetId, in TParam Params, MetricHeader MetricHeader)
    where TParam : unmanaged;

public readonly record struct GfxResourceMetric<TParam>(int ResourceId, in TParam Params, MetricHeader MetricHeader)
    where TParam : unmanaged;

public readonly record struct GlResourceMetric<TParam>(uint Handle, in TParam Params, MetricHeader MetricHeader)
    where TParam : unmanaged;

public readonly record struct CountMetrics(int Count, int Param0, int Param1 = 0, int Param2 = 0);

public readonly record struct SizeMetrics(
    Size2D Size,
    int Param0,
    int Param1 = 0,
    float FParam0 = 0,
    float FParam1 = 0);

public readonly record struct CapacityMetrics(long Capacity, long Size, int Param0 = 0, int Param1 = 0);

public readonly record struct BufferMetrics(long Capacity, long Stride, int Param0, int Param1 = 0, int Param2 = 0);

public readonly record struct VectorMetrics(Vector4 Vec4, int Param0, int Param1 = 0, int Param2 = 0);

public readonly record struct ColorMetrics(Color4 Color, int Param0, int Param1 = 0, int Param2 = 0);

public readonly record struct MixedMetrics(
    long Value,
    int Param0,
    int Param1 = 0,
    float FParam0 = 0,
    float FParam1 = 0,
    float FParam2 = 0);