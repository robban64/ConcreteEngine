using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class PassTagKeyNativeComparer : IComparer<PassTextureSlotKey>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(PassTextureSlotKey a, PassTextureSlotKey b)
    {
        if (a.TagIndex != b.TagIndex) return a.TagIndex < b.TagIndex ? -1 : 1;
        if (a.Slot != b.Slot) return a.Slot < b.Slot ? -1 : 1;
        if (a.PassOp != b.PassOp) return a.PassOp < b.PassOp ? -1 : 1;
        if (a.TextureSlot != b.TextureSlot) return a.TextureSlot < b.TextureSlot ? -1 : 1;
        return 0;
    }
}

public readonly record struct PassTagKey(Type TagType, Type SlotType, PassOpKind PassOp)
{
    public FboTagKey ToFboTagKey(int version) => new(TagType, SlotType, version);

    public static PassTagKey Make<TTag, TSlot>(PassOpKind passOp)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(typeof(TTag), typeof(TSlot), passOp);
}

public readonly record struct PassTextureSlotKey(int TagIndex, int Slot, PassOpKind PassOp, byte TextureSlot)
{
    public static PassTextureSlotKey Make<TTag, TSlot>(PassOpKind passOp, byte textureSlot)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(RTypeRegistry.RenderPassTag<TTag>.TagIndex, RTypeRegistry.RenderPassSlot<TSlot>.Slot, passOp, textureSlot);
}

public readonly record struct FboTagKey(Type TagType, Type SlotType, int Version)
{
    public static FboTagKey Make<TTag, TSlot>(int version)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(typeof(TTag), typeof(TSlot), version);
}