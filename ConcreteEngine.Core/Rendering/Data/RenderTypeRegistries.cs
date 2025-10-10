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


    internal static FboTagKey MakeFboTagKey<TTag>(byte variant) where TTag : unmanaged, IRenderPassTag 
        => new(RenderPassTag<TTag>.Index, variant);


    internal static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        public static int Index { get; private set; } = -1;

        public static void Register() => Index = _renderPassTagCounter++;
    }

    internal static class UniformBufferTag<IUbo> where IUbo : unmanaged, IStd140Uniform
    {
        public static UboSlot Slot { get; private set; } = new(-1);

        public static UboSlot RegisterSlot() => Slot = new UboSlot(_uboSlotCounter++);
    }
}