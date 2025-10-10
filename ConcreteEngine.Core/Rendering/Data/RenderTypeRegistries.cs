#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

internal static class TagRegistry
{
    private static int _renderPassTagCounter = 0;
    private static int _uboSlotCounter = 0;

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

    public static void RegisterTag<TTag>() where TTag : unmanaged, IRenderPassTag 
        => RenderPassTag<TTag>.RegisterTag();

    private static class RenderPassTag<TTag> where TTag : unmanaged, IRenderPassTag
    {
        private const int MaxVariants = 4;

        public static int TagIndex { get; private set; } = -1;

        private static PassId[] _passIds = new PassId[MaxVariants];

        public static PassId GetPassId(FboVariant variant) => _passIds[variant];

        public static void BindFboPassId(FboVariant variant, PassId passId)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(variant.Value, MaxVariants);

            if (TagIndex < 0)
                throw new InvalidOperationException($"PassTag not registered. {typeof(TTag).Name}");

            if (_passIds[variant] != default) throw new InvalidOperationException(nameof(variant));

            _passIds[variant] = passId;
        }

        public static void RegisterTag()
        {
            if (TagIndex >= 0) 
                throw new InvalidOperationException($"PassTag already registered. {typeof(TTag).Name}");
            
            TagIndex = _renderPassTagCounter++;
        }
    }

    internal static class UniformBufferTag<IUbo> where IUbo : unmanaged, IStd140Uniform
    {
        public static UboSlot Slot { get; private set; } = new(-1);

        public static UboSlot RegisterSlot()
        {
            if(Slot >= 0)
                throw new InvalidOperationException($"UboTag already registered. {typeof(IUbo).Name}");

            return Slot = new UboSlot(_uboSlotCounter++);
        }
    }
}