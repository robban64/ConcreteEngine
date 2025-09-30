using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public readonly record struct UboSlotKey<T>(UniformBufferId UboId, UboSlot Slot) where T : unmanaged, IUniformGpuData
{
    internal static UboSlotKey<T> Make(UniformBufferId uboId, int value) => new(uboId, new UboSlot(value));
}

public readonly record struct PassTagKey(Type TagType, Type SlotType, PassOpKind PassOp)
{
    public FboTagKey ToFboTagKey( int version) => new (TagType, SlotType, version);

    public static PassTagKey Make<TTag, TSlot>(PassOpKind passOp)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(typeof(TTag), typeof(TSlot), passOp);
    

    public static KeyNative MakeNative<TTag, TSlot>(PassOpKind passOp)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(RTypeRegistry.RenderPassTag<TTag>.TagIndex, RTypeRegistry.RenderPassSlot<TSlot>.Slot, passOp);


    public readonly record struct KeyNative(int TagIndex, int Slot, PassOpKind PassOp);
}

public readonly record struct FboTagKey(Type TagType, Type SlotType, int Version)
{
    public static FboTagKey Make<TTag, TSlot>(int version)
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot =>
        new(typeof(TTag), typeof(TSlot), version);

}
