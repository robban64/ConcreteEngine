using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Render.Passes;

public readonly record struct FboVariant(byte Value) : IComparable<FboVariant>
{
    public static readonly FboVariant V0 = new(0);
    public static readonly FboVariant V1 = new(1);

    public static implicit operator byte(FboVariant slot) => slot.Value;

    public int CompareTo(FboVariant other) => Value.CompareTo(other.Value);
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

public readonly record struct PassId(byte Value) : IComparable<PassId>
{
    public PassId(int value) : this((byte)value) { }
    public static implicit operator int(PassId id) => id.Value;
    public int CompareTo(PassId other) => Value.CompareTo(other.Value);
}

public readonly record struct PassTargetKey(byte TagIndex, FboVariant Variant, PassId Pass) : IComparable<PassTargetKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(PassTargetKey other)
    {
        var c = Pass.CompareTo(other.Pass);
        if (c != 0) return c;

        c = TagIndex.CompareTo(other.TagIndex);
        return c != 0 ? c : Variant.CompareTo(other.Variant);
    }
}

public readonly record struct PassTextureSlotKey(byte TagIndex, FboVariant Variant, PassId Pass, byte TextureSlot)
    : IComparable<PassTextureSlotKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(PassTextureSlotKey other)
    {
        var c = Pass.CompareTo(other.Pass);
        if (c != 0) return c;

        c = TagIndex.CompareTo(other.TagIndex);
        if (c != 0) return c;

        c = Variant.CompareTo(other.Variant);
        return c != 0 ? c : TextureSlot.CompareTo(other.TextureSlot);
    }
}
