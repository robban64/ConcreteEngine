using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Handles;

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

    public static void OnUboChanged(in GfxMetaChanged<UniformBufferMeta> message)
    {
        var id = new UniformBufferId(message.Id);
        InvalidOpThrower.ThrowIfNull(_uboRegistry, nameof(_uboRegistry));
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Value, 1, nameof(id));

        var renderUbo = _uboRegistry.GetBySlot(message.Meta.Slot);
        renderUbo.SetCapacity(message.Meta.Capacity);
    }


    public static void OnFboChange(in GfxMetaChanged<FrameBufferMeta> message)
    {
        var id = new FrameBufferId(message.Id);
        InvalidOpThrower.ThrowIfNull(_fboRegistry, nameof(_fboRegistry));
        ArgumentOutOfRangeException.ThrowIfLessThan(id.Value, 1, nameof(id));
        var renderFbo = _fboRegistry.GetRenderFboById(id);
        if (renderFbo is null) RenderFboRegistry.ThrowNotFound(id);
        renderFbo.UpdateFromMeta(in message.Meta);
    }
}