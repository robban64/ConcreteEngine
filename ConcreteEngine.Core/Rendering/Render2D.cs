using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;


namespace ConcreteEngine.Core.Rendering;



internal class Render2D : IRender
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly RenderTargetRegistry _registry;
    private readonly MaterialStore _materialStore;
    private readonly Camera2D _camera;
    public ICamera Camera => _camera;

    public RenderTargetEnumerator GetEnumerator() => _registry.GetEnumerator();


    public Render2D(IGraphicsDevice graphics, MaterialStore  materialStore)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
        _materialStore = materialStore;
        _camera = new Camera2D();
        _registry = new RenderTargetRegistry(graphics);
    }


    public void PrepareRender(float alpha)
    {
        var projectionViewMatrix = _camera.ProjectionViewMatrix;
        foreach (var material in _materialStore.Materials)
        {
            if (material.HasViewProjection)
            {
                _gfx.UseShader(material.ShaderId);
                _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projectionViewMatrix);
            }
        }
    }

    public void RenderScenePass(SceneRenderPass pass, DrawCommandSubmitter submitter)
    {
        submitter.DrainCommandQueue(RenderTargetId.Scene);
    }

    public void RenderLightPass(LightRenderPass lightPass, DrawCommandSubmitter submitter)
    {
        _gfx.UseShader(lightPass.Shader);
        submitter.DrainCommandQueue(RenderTargetId.SceneLight);
    }

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _registry.MutateRenderPass(targetId, mutation);

    public void RegisterRenderTargetsFrom(RenderTargetDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        desc.LightTarget.LightShader.IsValidOrThrow();
        desc.ScreenTarget.CompositeShaderId.IsValidOrThrow();

        // Scene Target setup
        var sceneTarget = desc.SceneTarget;
        _registry.CreateSceneBuffer();
        _registry.CreateMultisampleBuffer(Vector2.One, sceneTarget.Samples);

        // Light Target setup
        var lightTarget = desc.LightTarget;
        _registry.CreateLightBuffer(lightTarget.SizeRatio, lightTarget.TexPreset);


        // Screen Target setup
        var screenTarget = desc.ScreenTarget;

        // Scene Passes
        // Pass 0: draw scene into MSAA FBO
        _registry.RegisterRenderPass(RenderTargetId.Scene, new SceneRenderPass
        {
            TargetFbo = _registry.MultisampleFbo.FboId,
            Clear = new RenderPassClearDesc(Colors.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
        });

        // Pass 1: resolve MSAA into single-sample texture FBO
        _registry.RegisterRenderPass(RenderTargetId.Scene, new BlitRenderPass
        {
            TargetFbo = _registry.SceneFbo.FboId,
            BlitFbo = _registry.MultisampleFbo.FboId,
            Multisample = true,
            Samples = desc.SceneTarget.Samples
        });

        // Light Passes
        // Pass 0: Draw light into FBO
        _registry.RegisterRenderPass(RenderTargetId.SceneLight, new LightRenderPass
        {
            TargetFbo = _registry.LightFbo.FboId,
            Shader = lightTarget.LightShader,
            Clear = new RenderPassClearDesc(lightTarget.ClearColor, ClearBufferFlag.Color),
            Blend = lightTarget.Blend,
            DepthTest = false
        });

        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        _registry.RegisterRenderPass(RenderTargetId.Screen, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [_registry.SceneFbo.FboMeta.ColTexId, _registry.LightFbo.FboMeta.ColTexId],
            Shader = screenTarget.CompositeShaderId
        });
    }
}