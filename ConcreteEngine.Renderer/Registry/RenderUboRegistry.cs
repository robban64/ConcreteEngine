#region

using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Renderer.Registry;

internal sealed class RenderUboRegistry
{
    private readonly RenderUbo[] _uboRegistry = new RenderUbo[RenderLimits.UboSlots];
    private int _uboCount = 0;

    private readonly GfxResourceApi _gfxApi;
    private readonly GfxBuffers _gfxBuffers;

    internal RenderUboRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceManager.GetGfxApi();
        _gfxBuffers = gfx.Buffers;
    }

    public void BeginRegistration()
    {
        Register<EngineUniformRecord, EngineUboTag>();
        Register<FrameUniformRecord, FrameUboTag>();
        Register<CameraUniformRecord, CameraUboTag>();
        Register<DirLightUniformRecord, DirLightUboTag>();
        Register<LightUniformRecord, LightUboTag>();
        Register<ShadowUniformRecord, ShadowUboTag>();
        Register<MaterialUniformRecord, MaterialUboTag>();
        Register<DrawObjectUniform, DrawUboTag>();
        Register<PostProcessUniform, PostUboTag>();
    }

    public void FinishRegistration()
    {
    }

    public void Register<TUbo, TTag>() where TTag : class where TUbo : unmanaged
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