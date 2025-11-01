#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Common.Diagnostics;

public readonly record struct UtilizationSample(int Used, int Total, int Param1 = 0);

// Collection samples
public readonly record struct CollectionSample(int Count, int Capacity, int Active, int Reserved = 0);

public readonly record struct CapacitySample(long Capacity, long Used, int Headroom = 0, int Hint = 0);

public readonly record struct BufferSample(long Capacity, long Size, int Stride, int Param0, int Param1 = 0);

// App samples
public readonly record struct RenderInfoSample(float Fps, float Alpha, int Passes, int Draws, int Tris);

// Generic samples
public readonly record struct ScalarSample(float Value, int Param0 = 0, int Param1 = 0);

public readonly record struct PairSample(int Value, int Param0 = 0);

public readonly record struct ValueSample(long Value, int Param0 = 0, int Param1 = 0);

public readonly record struct RangeSample(Range32 Range, int Param0 = 0);

public readonly record struct Vector2Sample(Vector2 Values, Vector2I Params);

public readonly record struct Vector4Metric(Vector4 Values, Vector2I Params);

public readonly record struct ColorSample(Color4 Color, int Param0, int Param1 = 0);

public readonly record struct Size2Sample(Size2D Size, int Param0, int Param1 = 0, float FParam0 = 0);

public readonly record struct MixedSample(
    long Value,
    int Param0,
    int Param1 = 0,
    float FParam0 = 0,
    float FParam1 = 0);