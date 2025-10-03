#region

using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

internal static class RTypeRegistry
{
    private static int _renderPassTagCounter = 0;
    private static int _renderPassSlotCounter = 0;

    private static int _uboSlotCounter = 0;


    internal static FboTagKey MakeFboTagKey<TTag, TSlot>()
        where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
        => new(RenderPassTag<TTag>.Index, RenderPassSlot<TSlot>.Index);


    internal static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        public static int Index { get; private set; } = -1;

        public static void Register() => Index = _renderPassTagCounter++;
    }

    internal static class RenderPassSlot<TSlot> where TSlot : unmanaged, IRenderPassTagSlot
    {
        public static int Index { get; private set; } = -1;

        public static void Register() => Index = _renderPassSlotCounter++;
    }

    internal static class UniformBufferTag<IUbo> where IUbo : unmanaged, IUniformGpuData
    {
        public static UboSlot Slot { get; private set; } = new(-1);

        public static UboSlot RegisterSlot() => Slot = new UboSlot(_uboSlotCounter++);
    }
}