using System.Runtime.CompilerServices;

namespace ConcreteEngine.Renderer.Passes;

public readonly record struct FboVariant(byte Value) : IComparable<FboVariant>
{
    public static readonly FboVariant Default = new(0);
    public static readonly FboVariant Secondary = new(1);

    public static implicit operator byte(FboVariant slot) => slot.Value;

    public int CompareTo(FboVariant other) => Value.CompareTo(other.Value);
}

public readonly record struct FboTagKey(int TagIndex, FboVariant Variant) : IComparable<FboTagKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(FboTagKey other)
    {
        var c = TagIndex.CompareTo(other.TagIndex);
        return c != 0 ? c : Variant.CompareTo(other.Variant);
    }
}

public readonly record struct PassTagKey(int TagIndex, FboVariant Variant, PassId Pass);

public readonly record struct PassTextureSlotKey(int TagIndex, FboVariant Variant, PassId Pass, byte TextureSlot);

public sealed class PassTagKeyComp : IComparer<PassTagKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PassTagKey a, PassTagKey b)
    {
        var c = a.Pass.CompareTo(b.Pass);
        if (c != 0) return c;

        c = a.TagIndex.CompareTo(b.TagIndex);
        return c != 0 ? c : a.Variant.CompareTo(b.Variant);
    }
}

public sealed class PassTextureSlotKeyComp : IComparer<PassTextureSlotKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PassTextureSlotKey a, PassTextureSlotKey b)
    {
        var c = a.Pass.CompareTo(b.Pass);
        if (c != 0) return c;

        c = a.TagIndex.CompareTo(b.TagIndex);
        if (c != 0) return c;

        c = a.Variant.CompareTo(b.Variant);
        return c != 0 ? c : a.TextureSlot.CompareTo(b.TextureSlot);
    }
}