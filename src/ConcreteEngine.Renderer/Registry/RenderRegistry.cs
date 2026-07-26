using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderRegistry
{
    public static ShaderId DepthShader { get; set; }
    public static ShaderId CompositeShader { get; set; }
    public static ShaderId ColorFilterShader { get; set; }
    public static ShaderId PresentShader { get; set; }
    public static ShaderId HighlightShader { get; set; }
    public static ShaderId BoundingBoxShader { get; set; }

    public readonly RenderFboRegistry FboRegistry;

    internal RenderRegistry(GfxContext gfx)
    {
        FboRegistry = new RenderFboRegistry(gfx);
    }

    internal void BeginRegistration(GfxBuffers gfxBuffers, Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        RegisterUbo(gfxBuffers);
        FboRegistry.BeginRegistration(outputSize);
    }

    internal void FinishRegistration()
    {
        FboRegistry.FinishRegistration();
        GfxResourceApi.BindMetaChanged<FrameBufferMeta>(FboRegistry.OnFboChange);
    }

    private static void RegisterUbo(GfxBuffers gfxBuffers)
    {
        EngineUniformRecord.UboId = gfxBuffers.CreateUniformBuffer<EngineUniformRecord>();
        FrameUniform.UboId = gfxBuffers.CreateUniformBuffer<FrameUniform>();
        CameraUniform.UboId = gfxBuffers.CreateUniformBuffer<CameraUniform>();
        DirectionalLightUniform.UboId = gfxBuffers.CreateUniformBuffer<DirectionalLightUniform>();
        LightUniform.UboId = gfxBuffers.CreateUniformBuffer<LightUniform>();
        ShadowUniform.UboId = gfxBuffers.CreateUniformBuffer<ShadowUniform>();
        MaterialUniform.UboId = gfxBuffers.CreateUniformBuffer<MaterialUniform>();
        DrawObjectUniform.UboId = gfxBuffers.CreateUniformBuffer<DrawObjectUniform>();
        DrawAnimationUniform.UboId = gfxBuffers.CreateUniformBuffer<DrawAnimationUniform>();
        PostFxUniform.UboId = gfxBuffers.CreateUniformBuffer<PostFxUniform>();
        EditorEffectsUniform.UboId = gfxBuffers.CreateUniformBuffer<EditorEffectsUniform>();
    }

}