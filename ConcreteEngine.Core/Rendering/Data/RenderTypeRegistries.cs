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

    private static readonly Dictionary<Type, int> RenderPassTagValues = new();
    private static readonly Dictionary<Type, int> RenderPassTagSlots = new();

    internal static int GetPassTagValue(Type type) => RenderPassTagValues[type];
    internal static int GetPassSlotValue(Type type) => RenderPassTagSlots[type];

    internal static (int, int) GetPassTagSlotTuple(Type tag, Type slot) =>
        (RenderPassTagValues[tag], RenderPassTagSlots[slot]);


    internal static class UniformBufferTag<IUbo> where IUbo : unmanaged, IUniformGpuData
    {
        public static UboSlot Slot { get; set; } = new(-1);
    }

    internal static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        public static int TagIndex { get; private set; } = -1;

        public static void Register()
        {
            RenderPassTagValues.Add(typeof(TTag), _renderPassTagCounter);
            TagIndex = _renderPassTagCounter++;
        }
    }

    internal static class RenderPassSlot<TSlot> where TSlot : unmanaged, IRenderPassTagSlot
    {
        public static int Slot { get; private set; } = -1;

        public static void Register()
        {
            RenderPassTagSlots.Add(typeof(TSlot), _renderPassSlotCounter);
            Slot = _renderPassSlotCounter++;
        }
    }
/*
    internal static class RenderPassTag<TTag, TSlot> where TTag : unmanaged, IRenderPassTag where TSlot : unmanaged, IRenderPassTagSlot
    {
        public static int TagIndex {get; set;} = -1;
        public static int Slot {get; set;} = -1;
    }
    */
}