using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Shared.Diagnostics;

//// General samples ////

// Collection samples
public readonly struct CollectionSample(int count, int capacity, int active, int reserved = 0)
{
    public readonly int Count = count;
    public readonly int Capacity = capacity;
    public readonly int Active = active;
    public readonly int Reserved = reserved;
}

public readonly struct CapacitySample(long capacity, long used, int headroom = 0, int hint = 0)
{
    public readonly long Capacity = capacity;
    public readonly long Used = used;
    public readonly int Headroom = headroom;
    public readonly int Hint = hint;
}

public readonly struct BufferSample(long capacity, long size, int stride, int param0)
{
    public readonly long Capacity = capacity;
    public readonly long Size = size;
    public readonly int Stride = stride;
    public readonly int Param0 = param0;
}

// Data samples

public readonly struct PairSample(int value1, int value2)
{
    public readonly int Value1 = value1;
    public readonly int Value2 = value2;
}

public readonly struct ValueSample(long value, int param0 = 0, int param1 = 0)
{
    public readonly long Value = value;
    public readonly int Param0 = param0;
    public readonly int Param1 = param1;
}

/*
public readonly struct UtilizationSample(int used, int total, int param1 = 0)
{
    public readonly int Used = used;
    public readonly int Total = total;
    public readonly int Param1 = param1;
}

public readonly struct ScalarSample(float value, int param0 = 0, int param1 = 0)
{
    public readonly float Value = value;
    public readonly int Param0 = param0;
    public readonly int Param1 = param1;
}



public readonly struct RangeSample(Range32 range, int param0 = 0)
{
    public readonly Range32 Range = range;
    public readonly int Param0 = param0;
}

public readonly struct VectorSample(in Vector4 values, Vector2I param)
{
    public readonly Vector4 Values = values;
    public readonly Vector2I Param = param;
}*/