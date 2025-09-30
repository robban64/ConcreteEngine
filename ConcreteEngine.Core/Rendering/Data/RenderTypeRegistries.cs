using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

internal static class RTypeRegistry
{
    private static int _renderPassTagCounter = 0;
    private static int _renderPassSlotCounter = 0;

    internal static class UniformBufferTag<IUbo> where IUbo : unmanaged, IUniformGpuData
    {
        public static UboSlot Slot { get; set; } = new (-1);
    }
    
    internal static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        public static int TagIndex {get; private set; } = -1;
        public static void Register() => TagIndex = _renderPassTagCounter++;
    }
    
    internal static class RenderPassSlot<TSlot> where TSlot : unmanaged, IRenderPassTagSlot
    {
        public static int Slot {get; private set; } = -1;
        public static void Register() => Slot = _renderPassSlotCounter++;

    }
/*
    internal static class RenderPassTag<TTag, TSlot> where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        public static int TagIndex {get; set;} = -1;
        public static int Slot {get; set;} = -1;
    }
    */
}