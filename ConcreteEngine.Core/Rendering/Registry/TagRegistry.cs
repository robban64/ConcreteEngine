#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Core.Rendering.Data.RenderLimits;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

internal static class TagRegistry
{
    private static int _renderPassTagCounter = 0;
    private static int _uboSlotCounter = 0;

    //Pass Tag

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TagIndexOf<TTag>() where TTag : unmanaged, IRenderPassTag => RenderPassTag<TTag>.TagIndex;

    public static FboTagKey FboKey<TTag>(FboVariant variant) where TTag : unmanaged, IRenderPassTag =>
        new(TagIndexOf<TTag>(), variant);

    public static PassTagKey PassKey<TTag>(FboVariant variant) where TTag : unmanaged, IRenderPassTag =>
        new(TagIndexOf<TTag>(), variant, RenderPassTag<TTag>.GetPassId(variant));

    public static PassTagKey BindFboPassId<TTag>(FboVariant variant, PassId passId)
        where TTag : unmanaged, IRenderPassTag
    {
        RenderPassTag<TTag>.BindFboPassId(variant, passId);
        return PassKey<TTag>(variant);
    }

    public static void RegisterTag<TTag>() where TTag : unmanaged, IRenderPassTag => RenderPassTag<TTag>.RegisterTag();

    //Ubo
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UboSlot UniformBufferSlot<TUbo>() where TUbo : unmanaged, IStd140Uniform =>
        UniformBufferTag<TUbo>.Slot;

    public static UboSlot RegisterUniformBufferSlot<TUbo>() where TUbo : unmanaged, IStd140Uniform =>
        UniformBufferTag<TUbo>.RegisterSlot();


    //
    private static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        internal static int TagIndex { get; private set; } = -1;

        private static PassId[] _passIds = new PassId[MaxFboVariants];

        public static PassId GetPassId(FboVariant variant) => _passIds[variant];

        public static void BindFboPassId(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, MaxFboVariants);

            if (TagIndex < 0)
                throw new InvalidOperationException($"PassTag not registered. {typeof(TTag).Name}");

            if (_passIds[variant] != default) throw new InvalidOperationException(nameof(variant));

            _passIds[variant] = passId;
        }

        public static void RegisterTag()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_renderPassTagCounter, FboSlots);

            if (TagIndex >= 0)
                throw new InvalidOperationException($"PassTag already registered. {typeof(TTag).Name}");

            TagIndex = _renderPassTagCounter++;
        }
    }

    private static class UniformBufferTag<TUbo> where TUbo : unmanaged, IStd140Uniform
    {
        internal static UboSlot Slot { get; private set; } = new(-1);

        public static UboSlot RegisterSlot()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_uboSlotCounter, UboSlots);

            if (Slot >= 0)
                throw new InvalidOperationException($"UboTag already registered. {typeof(TUbo).Name}");

            return Slot = new UboSlot(_uboSlotCounter++);
        }
    }
}