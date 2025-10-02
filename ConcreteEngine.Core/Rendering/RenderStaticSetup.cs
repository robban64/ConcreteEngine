using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;

namespace ConcreteEngine.Core.Rendering;

internal static class RenderStaticSetup
{
    internal static void RegisterPassTagTypes()
    {
        RTypeRegistry.RenderPassTag<ScenePassTag>.Register();
        RTypeRegistry.RenderPassTag<ShadowPassTag>.Register();
        RTypeRegistry.RenderPassTag<LightPassTag>.Register();
        RTypeRegistry.RenderPassTag<PostPassTag>.Register();
        RTypeRegistry.RenderPassTag<ScreenPassTag>.Register();

        
    }
    
    internal static void RegisterPassSlotTypes()
    {
        RTypeRegistry.RenderPassSlot<PassDrawSlot>.Register();
        RTypeRegistry.RenderPassSlot<PassResolveSlot>.Register();
        RTypeRegistry.RenderPassSlot<PassPostASlot>.Register();
        RTypeRegistry.RenderPassSlot<PassPostBSlot>.Register();
        RTypeRegistry.RenderPassSlot<PassFinalSlot>.Register();
    }

    
    internal static void RegisterUniformBufferTypes(RenderRegistry _renderRegistry)
    {
        
    }
}