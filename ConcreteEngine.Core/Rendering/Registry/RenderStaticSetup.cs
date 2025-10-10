#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Descriptors;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

// Placeholder
internal static class RenderStaticSetup
{
    internal static void RegisterFrameBuffers(RenderRegistry renderRegistry)
    {
        renderRegistry.RegisterFrameBuffer<ShadowPassTag>(FboVariant.Default,
            RegisterFboEntry.MakeDefault(false).AttachDepthTexture().UseFixedSize(new Size2D(2048, 2048))
        );
        renderRegistry.RegisterFrameBuffer<ScenePassTag>(FboVariant.Default,
            RegisterFboEntry.MakeMsaa(RenderBufferMsaa.X4).AttachColorTexture().AttachDepthStencilBuffer()
        );
        renderRegistry.RegisterFrameBuffer<ScenePassTag>(FboVariant.Secondary,
            RegisterFboEntry.MakeDefault(true).AttachColorTexture().AttachDepthStencilBuffer()
        );
        renderRegistry.RegisterFrameBuffer<PostPassTag>(FboVariant.Default,
            RegisterFboEntry.MakePost(false).AttachColorTexture()
        );
        renderRegistry.RegisterFrameBuffer<PostPassTag>(FboVariant.Secondary,
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
        TagRegistry.RegisterTag<ShadowPassTag>();
        TagRegistry.RegisterTag<ScenePassTag>();
        TagRegistry.RegisterTag<LightPassTag>();
        TagRegistry.RegisterTag<PostPassTag>();
        TagRegistry.RegisterTag<ScreenPassTag>();
    }
}