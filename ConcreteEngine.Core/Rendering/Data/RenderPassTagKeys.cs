#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct FboTagKey(int TagIndex, int SlotIndex) : IComparable<FboTagKey>
{
    
    public static FboTagKey Make<TTag, TSlot>()
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        RTypeRegistry.MakeFboTagKey<TTag, TSlot>();

    public int CompareTo(FboTagKey other)
    {
        var c = TagIndex.CompareTo(other.TagIndex);
        return c != 0 ? c : SlotIndex.CompareTo(other.SlotIndex);
    }
}

public readonly record struct PassTagKey(FboTagKey FboTagKey, PassOpKind PassOp)
{
    public static PassTagKey Make<TTag, TSlot>(PassOpKind passOp)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(FboTagKey.Make<TTag, TSlot>(), passOp);
    
    public static implicit operator FboTagKey(PassTagKey passTagKey) => passTagKey.FboTagKey;

    public int TagIndex => FboTagKey.TagIndex;
    public int SlotIndex => FboTagKey.SlotIndex;
}

public readonly record struct PassTextureSlotKey(PassTagKey PassTagKey, byte TextureSlot)
{
    public static PassTextureSlotKey Make<TTag, TSlot>(PassOpKind passOp, byte TextureSlot)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(PassTagKey.Make<TTag, TSlot>(passOp), TextureSlot);

    public int TagIndex => PassTagKey.TagIndex;
    public int SlotIndex => PassTagKey.SlotIndex;
    public PassOpKind PassOp => PassTagKey.PassOp;
}

public sealed class PassTagKeyComp : IComparer<PassTagKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PassTagKey a, PassTagKey b)
    {
        if (a.TagIndex != b.TagIndex) return a.TagIndex < b.TagIndex ? -1 : 1;
        if (a.SlotIndex != b.SlotIndex) return a.SlotIndex < b.SlotIndex ? -1 : 1;
        if (a.PassOp != b.PassOp) return a.PassOp < b.PassOp ? -1 : 1;
        return 0;
    }
}

public sealed class PassTextureSlotKeyComp : IComparer<PassTextureSlotKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PassTextureSlotKey a, PassTextureSlotKey b)
    {
        if (a.TagIndex != b.TagIndex) return a.TagIndex < b.TagIndex ? -1 : 1;
        if (a.SlotIndex != b.SlotIndex) return a.SlotIndex < b.SlotIndex ? -1 : 1;
        if (a.PassOp != b.PassOp) return a.PassOp < b.PassOp ? -1 : 1;
        if (a.TextureSlot != b.TextureSlot) return a.TextureSlot < b.TextureSlot ? -1 : 1;
        return 0;
    }
}