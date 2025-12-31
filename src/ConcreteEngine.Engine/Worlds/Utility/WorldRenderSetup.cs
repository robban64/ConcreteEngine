using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Descriptors;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Engine.Worlds.Utility;

internal static class WorldRenderSetup
{
    internal static void RegisterFrameBuffers(RenderSetupBuilder builder, WorldVisual worldVisual)
    {
        builder.RegisterFbo<ShadowPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachDepthTexture(FboDepthAttachment.Default())
                .UseFixedSize(new Size2D(worldVisual.ShadowMapSize)));

        builder.RegisterFbo<ScenePassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Off(), RenderBufferMsaa.X4)
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<ScenePassTag>(FboVariant.Secondary,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.DefaultMip())
                .AttachDepthStencilBuffer());

        builder.RegisterFbo<PostPassTag>(FboVariant.Default,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));

        builder.RegisterFbo<PostPassTag>(FboVariant.Secondary,
            new RegisterFboEntry().AttachColorTexture(FboColorAttachment.Default()));
    }

    internal static RenderCoreShaders GetCoreShaders(AssetStore store) =>
        new()
        {
            DepthShader = store.GetByName<Shader>("Depth").ResourceId,
            ColorFilterShader = store.GetByName<Shader>("ColorFilter").ResourceId,
            CompositeShader = store.GetByName<Shader>("Composite").ResourceId,
            PresentShader = store.GetByName<Shader>("Present").ResourceId,
            HighlightShader = store.GetByName<Shader>("Highlight").ResourceId,
            BoundingBoxShader = store.GetByName<Shader>("BoundingBox").ResourceId,
            ParticleShader = store.GetByName<Shader>("Particle").ResourceId,
        };
}