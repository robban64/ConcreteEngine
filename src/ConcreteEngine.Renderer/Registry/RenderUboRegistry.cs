using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Data;

// ReSharper disable StaticMemberInGenericType

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderUboRegistry
{
    private static int _uboSlotCounter;

    private static class TypeRegistry<TUbo> where TUbo : unmanaged,IUniform
    {
        public static UniformBufferId UboId = new(0);
        public static UboSlot Slot = new(uint.MaxValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UboSlot RegisterSlot()
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_uboSlotCounter, RenderLimits.UboSlots);

            if (Slot < uint.MaxValue || UboId.IsValid())
                throw new InvalidOperationException($"UboTag already registered. {typeof(TUbo).Name}");

            return Slot = new UboSlot((uint)_uboSlotCounter++);
        }
    }


    private readonly RenderUbo[] _uboRegistry = new RenderUbo[RenderLimits.UboSlots];
    private int _uboCount;

    private readonly GfxResourceApi _gfxApi;
    private readonly GfxBuffers _gfxBuffers;

    internal RenderUboRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceManager.GetGfxApi();
        _gfxBuffers = gfx.Buffers;
    }

    internal void OnUboChanged(int id)
    {
        var uboId = (UniformBufferId)id;
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);
        var renderUbo = GetBySlot(meta.Slot);
        renderUbo.SetCapacity(meta.Capacity);
    }

    internal void BeginRegistration()
    {
        Register<EngineUniformRecord>();
        Register<FrameUniform>();
        Register<CameraUniform>();
        Register<DirectionalLightUniform>();
        Register<LightUniform>();
        Register<ShadowUniform>();
        Register<MaterialUniform>();
        Register<DrawObjectUniform>();
        Register<DrawAnimationUniform>();
        Register<PostFxUniform>();
        Register<EditorEffectsUniform>();
    }

    internal void FinishRegistration()
    {
    }

    internal void Register<TUbo>() where TUbo : unmanaged,IUniform
    {
        var newSlot = TypeRegistry<TUbo>.RegisterSlot();
        InvalidOpThrower.ThrowIfNotNull(_uboRegistry[newSlot]);

        var uboId = TypeRegistry<TUbo>.UboId = _gfxBuffers.CreateUniformBuffer<TUbo>(newSlot);
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);

        _uboRegistry[newSlot] = new RenderUbo(uboId, newSlot, in meta);
        _uboCount++;
    }

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IUniform => _uboRegistry[TypeRegistry<TUbo>.Slot];
    private RenderUbo GetBySlot(UboSlot slot) => _uboRegistry[slot];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UniformBufferId GetUboId<TUbo>() where TUbo : unmanaged, IUniform => TypeRegistry<TUbo>.UboId;
}