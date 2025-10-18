#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Primitives;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

internal sealed class RenderUboRegistry 
{
    private readonly RenderUbo[] _uboRegistry = new RenderUbo[RenderLimits.UboSlots];
    private int _uboCount = 0;

    private readonly GfxResourceApi _gfxApi;
    private readonly GfxBuffers _gfxBuffers;

    internal RenderUboRegistry(GfxContext gfx)
    {
        _gfxApi = gfx.ResourceContext.ResourceManager.GetGfxApi();
        _gfxBuffers = gfx.Buffers;

        _gfxApi.BindMetaChanged<UniformBufferId, UniformBufferMeta>(OnUboChange);
    }

    public void BeginRegistration()
    {
        Register<EngineUniformRecord>();
        Register<FrameUniformRecord>();
        Register<CameraUniformRecord>();
        Register<DirLightUniformRecord>();
        Register<LightUniformRecord>();
        Register<ShadowUniformRecord>();
        Register<MaterialUniformRecord>();
        Register<DrawObjectUniform>();
        Register<PostProcessUniform>();
    }

    public void FinishRegistration()
    {
    }

    public void Register<TUbo>() where TUbo : unmanaged, IStd140Uniform
    {
        InvalidOpThrower.ThrowIfCapacityExceed(_uboRegistry, RenderLimits.UboSlots);
        if (!UniformBufferUtils.IsStd140Aligned<TUbo>())
            throw new InvalidOperationException($"{typeof(TUbo).Name} is not std140-aligned.");

        var newSlot = TagRegistry.RegisterUniformBufferSlot<TUbo>();
        InvalidOpThrower.ThrowIfNotNull(_uboRegistry[newSlot]);

        var uboId = _gfxBuffers.CreateUniformBuffer<TUbo>(newSlot);
        var meta = _gfxApi.GetMeta<UniformBufferId, UniformBufferMeta>(uboId);

        _uboRegistry[newSlot] = new RenderUbo(uboId, newSlot, in meta);
        _uboCount++;
    }

    public RenderUbo GetRenderUbo<TUbo>() where TUbo : unmanaged, IStd140Uniform
    {
        var slot = TagRegistry.UniformBufferSlot<TUbo>();
        return _uboRegistry[slot];
    }

    private void OnUboChange(UniformBufferId id, in GfxMetaChanged<UniformBufferMeta> message)
    {
        var meta = message.NewMeta;
        var renderUbo = _uboRegistry[meta.Slot];
        renderUbo.SetCapacity(meta.Capacity);
    }
}