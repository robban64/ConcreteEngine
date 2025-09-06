using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

internal sealed class Render3D: IRender
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly MaterialStore _materialStore;

    private readonly RenderPasses _registry;
    private readonly Camera3D _camera;

    public ICamera Camera => _camera;

    public RenderTargetEnumerator GetEnumerator() => _registry.GetEnumerator();
 


    public Render3D(IGraphicsDevice graphics, MaterialStore  materialStore)
    {
        _graphics = graphics;
        _gfx = _graphics.Gfx;
        _materialStore = materialStore;
        _camera = new Camera3D();
        _registry =  new RenderPasses(graphics);

    }
    
    
    public void PrepareRender(float alpha, in RenderGlobalSnapshot renderGlobals )
    {
        var projectionViewMatrix = _camera.ProjectionViewMatrix;
        
        
        foreach (var material in _materialStore.Materials)
        {
            if (material.HasViewProjection)
            {
                _gfx.UseShader(material.ShaderId);
                _gfx.SetUniform(ShaderUniform.ProjectionViewMatrix, in projectionViewMatrix);
            }
            
            if (material.HasCameraPos)
            {
                _gfx.UseShader(material.ShaderId);
                _gfx.SetUniform(ShaderUniform.CameraPos, _camera.Translation);
            }
            
            if (material.HasAmbient)
            {
                _gfx.UseShader(material.ShaderId);
                _gfx.SetUniform(ShaderUniform.Ambient,  renderGlobals.Ambient);
            }

            if (material.DirLightUniforms.HasValue)
            {
                var unforms = material.DirLightUniforms.Value;
                var exposure = renderGlobals.Exposure;
                _gfx.UseShader(material.ShaderId);
                _gfx.SetRawUniform(unforms.Direction, renderGlobals.DirLight.Direction );
                _gfx.SetRawUniform(unforms.Diffuse,  renderGlobals.DirLight.Diffuse );
                _gfx.SetRawUniform(unforms.Specular,  renderGlobals.DirLight.Specular );
                _gfx.SetRawUniform(unforms.Intensity,  renderGlobals.DirLight.Intensity );
            }
            
            

        }
        
    }

    public void RenderScenePass(SceneRenderPass pass, RenderPipeline submitter)
    {
        submitter.DrainCommandQueue(RenderTargetId.Scene);
    }

    public void RenderLightPass(LightRenderPass lightPass, RenderPipeline submitter)
    {
    }
    
    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation)
        => _registry.MutateRenderPass(targetId, mutation);
    
    

    public void RegisterRenderTargetsFrom(RenderTargetDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        desc.ScreenTarget.CompositeShaderId.IsValidOrThrow();

        // Scene Target setup
        var sceneTarget = desc.SceneTarget;
        _registry.CreateSceneBuffer();
        _registry.CreateMultisampleBuffer(Vector2.One, sceneTarget.Samples);

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

        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        _registry.RegisterRenderPass(RenderTargetId.Screen, new FsqRenderPass
        {
            TargetFbo = default,
            SourceTextures = [_registry.SceneFbo.FboMeta.ColTexId],
            Shader = screenTarget.CompositeShaderId
        });

    }


}