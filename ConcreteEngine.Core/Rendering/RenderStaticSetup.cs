using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

// Placeholder
internal static class RenderStaticSetup
{
    internal static void RegisterFrameBuffers(RenderRegistry renderRegistry)
    {
        renderRegistry.RegisterFrameBuffer<ShadowPassTag, PassDrawSlot>(
            RegisterFboEntry.MakeDefault(false).AttachDepthTexture().UseFixedSize(new Size2D(2048, 2048))
        );
        renderRegistry.RegisterFrameBuffer<ScenePassTag, PassDrawSlot>(
            RegisterFboEntry.MakeMsaa(RenderBufferMsaa.X4).AttachColorTexture().AttachDepthStencilBuffer()
        );
        renderRegistry.RegisterFrameBuffer<ScenePassTag, PassResolveSlot>(
            RegisterFboEntry.MakeDefault(true).AttachColorTexture().AttachDepthStencilBuffer()
        );
        renderRegistry.RegisterFrameBuffer<PostPassTag, PassPostASlot>(
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );
        renderRegistry.RegisterFrameBuffer<PostPassTag, PassPostBSlot>(
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );
    }


    internal static void RegisterUniformBufferTypes(RenderRegistry renderRegistry)
    {
        renderRegistry.RegisterUniformBuffer<EngineUniformRecord>();
        renderRegistry.RegisterUniformBuffer<FrameUniformRecord>();
        renderRegistry.RegisterUniformBuffer<CameraUniformRecord>();
        renderRegistry.RegisterUniformBuffer<DirLightUniformRecord>();
        renderRegistry.RegisterUniformBuffer<LightUniformRecord>();
        renderRegistry.RegisterUniformBuffer<ShadowUniformRecord>();
        renderRegistry.RegisterUniformBuffer<MaterialUniformRecord>();
        renderRegistry.RegisterUniformBuffer<DrawObjectUniform>();
        renderRegistry.RegisterUniformBuffer<PostProcessUniform>();
    }

    internal static void RegisterPassTagTypes()
    {
        RTypeRegistry.RenderPassTag<ShadowPassTag>.Register();
        RTypeRegistry.RenderPassTag<ScenePassTag>.Register();
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
}