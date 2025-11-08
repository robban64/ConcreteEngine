using System.Numerics;
using ConcreteEngine.Common.Numerics;

namespace ConcreteEngine.Shared.Diagnostics;

// Scoped samples
public readonly record struct RenderInfoSample(float Fps, float Alpha, int Passes, int Draws, int Tris);

//// General samples ////

// Collection samples
public readonly record struct CollectionSample(int Count, int Capacity, int Active, int Reserved = 0);

public readonly record struct CapacitySample(long Capacity, long Used, int Headroom = 0, int Hint = 0);

public readonly record struct BufferSample(long Capacity, long Size, int Stride, int Param0, int Param1 = 0);


// Data samples

public readonly record struct UtilizationSample(int Used, int Total, int Param1 = 0);

public readonly record struct ScalarSample(float Value, int Param0 = 0, int Param1 = 0);

public readonly record struct PairSample(int Value, int Param0 = 0);

public readonly record struct ValueSample(long Value, int Param0 = 0, int Param1 = 0);

public readonly record struct RangeSample(Range32 Range, int Param0 = 0);

public readonly record struct VectorSample(Vector4 Values, Vector2I Params);
