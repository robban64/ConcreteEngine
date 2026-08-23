using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Render.Passes;

public readonly record struct FboVariant(byte Value) : IComparable<FboVariant>
{
    public static FboVariant V0 => new(0);
    public static FboVariant V1 => new(1);
    public static FboVariant V3 => new(2);
    public static FboVariant V4 => new(3);

    public static implicit operator int(FboVariant slot) => slot.Value;

    public int CompareTo(FboVariant other) => Value.CompareTo(other.Value);
}

public readonly record struct PassId(byte Value) : IComparable<PassId>
{
    public PassId(int value) : this((byte)value) { }
    public static implicit operator int(PassId id) => id.Value;

    public int CompareTo(PassId other) => Value.CompareTo(other.Value);
}

public readonly record struct FboKey(byte TagIndex, FboVariant Variant) : IComparable<FboKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(FboKey other)
    {
        var c = TagIndex.CompareTo(other.TagIndex);
        return c != 0 ? c : Variant.CompareTo(other.Variant);
    }
}

public readonly record struct PassTargetKey(byte TagIndex, FboVariant Variant, PassId Pass) : IComparable<PassTargetKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator FboKey(PassTargetKey id) => new(id.TagIndex, id.Variant);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator PassId(PassTargetKey id) => id.Pass;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(PassTargetKey other)
    {
        var c = Pass.CompareTo(other.Pass);
        if (c != 0) return c;

        c = TagIndex.CompareTo(other.TagIndex);
        return c != 0 ? c : Variant.CompareTo(other.Variant);
    }
}