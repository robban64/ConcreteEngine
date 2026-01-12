using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderUboRegistry
{
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
        Register<EngineUniformRecord, EngineUboTag>();
        Register<FrameUniformRecord, FrameUboTag>();
        Register<CameraUniformRecord, CameraUboTag>();
        Register<DirLightUniformRecord, DirLightUboTag>();
        Register<LightUniformRecord, LightUboTag>();
        Register<ShadowUniformRecord, ShadowUboTag>();
        Register<MaterialUniformRecord, MaterialUboTag>();
        Register<DrawObjectUniform, DrawUboTag>();
        Register<DrawAnimationUniform, DrawAnimationUboTag>();
        Register<PostProcessUniform, PostUboTag>();
    }

    internal void FinishRegistration()
    {
    }

    internal void Register<TUbo, TTag>() where TTag : class where TUbo : unmanaged
    {
        InvalidOpThrower.ThrowIfCapacityExceed(_uboRegistry, RenderLimits.UboSlots);
        var newSlot = TagRegistry.RegisterUniformBufferSlot<TTag>();
        InvalidOpThrower.ThrowIfNotNull(_uboRegistry[newSlot]);

        var uboId = _gfxBuffers.CreateUniformBuffer<TUbo>(newSlot);
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);

        _uboRegistry[newSlot] = new RenderUbo(uboId, newSlot, in meta);
        _uboCount++;
    }

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : class
    {
        var slot = TagRegistry.UniformBufferSlot<TUbo>();
        return _uboRegistry[slot];
    }


    internal RenderUbo GetBySlot(UboSlot slot) => _uboRegistry[slot];
}