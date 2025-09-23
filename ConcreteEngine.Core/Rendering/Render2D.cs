using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;


namespace ConcreteEngine.Core.Rendering;



internal class Render2D : IRender
{
    private readonly GfxContext _gfx;
    private readonly RenderPasses _registry;
    private readonly MaterialStore _materialStore;
    private readonly Camera2D _camera;
    public ICamera Camera => _camera;


    public Render2D(GfxContext gfx, MaterialStore  materialStore, in RenderGlobalSnapshot snapshot)
    {
        _gfx = gfx;
        _materialStore = materialStore;
        _camera = new Camera2D();
        _registry = new RenderPasses(_gfx, in snapshot);
    }


    public void Prepare(float alpha, in RenderGlobalSnapshot snapshot)
    {

        var projectionViewMatrix = _camera.ProjectionViewMatrix;
        foreach (var material in _materialStore.Materials)
        {
        }
    }
    
    public bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes)
        => _registry.TryGetNextPasses(out targetId, out passes);


    public void RenderScenePass(IScenePass pass, RenderPipeline submitter)
    {
        if (pass is SceneRenderPass)
        {
            submitter.DrainCommandQueue(RenderTargetId.Scene);
        }
        else if (pass is LightRenderPass lightPass)
        {
            _gfx.Commands.UseShader(lightPass.Shader);
            submitter.DrainCommandQueue(RenderTargetId.Light);
        }
            
    }

    public void RenderDepthPass(IDepthPass depthPass, RenderPipeline submitter)
    {
    }

    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _registry.MutateRenderPass(targetId, mutation);

    public void RegisterRenderTargetsFrom(in Vector2D<int> outputSize, RenderTargetDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        desc.LightTarget.LightShaderId.IsValidOrThrow();
        desc.ScreenTarget.ScreenShaderId.IsValidOrThrow();

        
        
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
            Clear = new RenderPassClearDesc(Color4.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
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
        _registry.RegisterRenderPass(RenderTargetId.Light, new LightRenderPass
        {
            TargetFbo = _registry.LightFbo.FboId,
            Shader = lightTarget.LightShaderId,
            Clear = new RenderPassClearDesc(lightTarget.ClearColor, ClearBufferFlag.Color),
            Blend = lightTarget.Blend,
        });

        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        _registry.RegisterRenderPass(RenderTargetId.Screen, new IfsqPass
        {
            TargetFbo = default,
            SourceTextures = [_registry.SceneFbo.ColTexId, _registry.LightFbo.ColTexId],
            Shader = screenTarget.ScreenShaderId
        });
    }
}