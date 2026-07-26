using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderRegistry
{
    public readonly RenderShaderRegistry ShaderRegistry;
    public readonly RenderFboRegistry FboRegistry;
    private readonly GfxBuffers _gfxBuffers;

    internal RenderRegistry(GfxContext gfx)
    {
        ShaderRegistry = new RenderShaderRegistry();
        FboRegistry = new RenderFboRegistry(gfx);
        _gfxBuffers = gfx.Buffers;
    }

    internal void BeginRegistration(Size2D outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Width, 1, nameof(outputSize));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outputSize.Height, 1, nameof(outputSize));

        EngineUniformRecord.UboId = _gfxBuffers.CreateUniformBuffer<EngineUniformRecord>();
        FrameUniform.UboId = _gfxBuffers.CreateUniformBuffer<FrameUniform>();
        CameraUniform.UboId = _gfxBuffers.CreateUniformBuffer<CameraUniform>();
        DirectionalLightUniform.UboId = _gfxBuffers.CreateUniformBuffer<DirectionalLightUniform>();
        LightUniform.UboId = _gfxBuffers.CreateUniformBuffer<LightUniform>();
        ShadowUniform.UboId = _gfxBuffers.CreateUniformBuffer<ShadowUniform>();
        MaterialUniform.UboId = _gfxBuffers.CreateUniformBuffer<MaterialUniform>();
        DrawObjectUniform.UboId = _gfxBuffers.CreateUniformBuffer<DrawObjectUniform>();
        DrawAnimationUniform.UboId = _gfxBuffers.CreateUniformBuffer<DrawAnimationUniform>();
        PostFxUniform.UboId = _gfxBuffers.CreateUniformBuffer<PostFxUniform>();
        EditorEffectsUniform.UboId = _gfxBuffers.CreateUniformBuffer<EditorEffectsUniform>();

        FboRegistry.BeginRegistration(outputSize);
    }

    internal void FinishRegistration()
    {
        FboRegistry.FinishRegistration();
        ShaderRegistry.FinishRegistration();

        GfxResourceApi.BindMetaChanged<FrameBufferMeta>(FboRegistry.OnFboChange);
    }
    


}