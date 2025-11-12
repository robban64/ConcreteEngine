using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Renderer.Registry;

internal static class RenderRegistryGateway
{
    private static RenderFboRegistry _fboRegistry = null!;
    private static RenderUboRegistry _uboRegistry = null!;

    public static void Setup(RenderFboRegistry fboRegistry, RenderUboRegistry uboRegistry)
    {
        _fboRegistry = fboRegistry;
        _uboRegistry = uboRegistry;
    }
        
    public static void OnUboChanged(UniformBufferId id, in UniformBufferMeta newMeta, in UniformBufferMeta oldMeta, GfxMetaChanged message)
    {
        InvalidOpThrower.ThrowIfNull(_uboRegistry, nameof(_uboRegistry));
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Value, 1, nameof(id));
            
        var renderUbo = _uboRegistry.GetBySlot(newMeta.Slot);
        renderUbo.SetCapacity(newMeta.Capacity);
    }

        
    public static void OnFboChange(FrameBufferId id, in FrameBufferMeta newMeta, in FrameBufferMeta oldMeta, GfxMetaChanged message)
    {
        InvalidOpThrower.ThrowIfNull(_fboRegistry, nameof(_fboRegistry));
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Value, 1, nameof(id));
        var renderFbo = _fboRegistry.GetRenderFboById(id);
        if (renderFbo is null) RenderFboRegistry.ThrowNotFound(id);
        renderFbo.UpdateFromMeta(in newMeta);
    }
} 