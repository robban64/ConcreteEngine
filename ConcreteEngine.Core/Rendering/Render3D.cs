using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

internal sealed class Render3D : IRender
{
    private readonly IGraphicsRuntime _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly DrawProcessor _drawProcessor;

    private readonly RenderPasses _registry;
    private readonly Camera3D _camera;

    public ICamera Camera => _camera;

    public Render3D(IGraphicsRuntime graphics, DrawProcessor drawProcessor)
    {
        _graphics = graphics;
        _gfx = _graphics.Context;
        _drawProcessor = drawProcessor;
        _camera = new Camera3D();
        _registry = new RenderPasses(graphics);
    }


    public void Prepare(float alpha, in RenderGlobalSnapshot snapshot)
    {
        _registry.SetOutputSize(snapshot.OutputSize);

        var frameUniforms = new FrameUniformRecord(
            ambient: snapshot.Ambient,
            ambientIntensity: 1,
            fogColor: Vector3.One,
            fogDensity: 1,
            fogNear: 1,
            fogFar: 1,
            fogType: 1
        );

        var cameraUniforms = new CameraUniformRecord(
            viewId: default,
            viewMat: _camera.ViewMatrix,
            projMat: _camera.ProjectionMatrix,
            projViewMat: _camera.ProjectionViewMatrix,
            cameraPos: _camera.Translation
        );


        var dirLightUniforms = new DirLightUniformRecord(
            viewId: default,
            direction: snapshot.DirLight.Direction,
            diffuse: snapshot.DirLight.Diffuse,
            specular: snapshot.DirLight.Specular,
            intensity: snapshot.DirLight.Intensity
        );

        _drawProcessor.UploadFrame(in frameUniforms);
        _drawProcessor.UploadCamera(in cameraUniforms);
        _drawProcessor.UploadDirLight(in dirLightUniforms);

    }

    public bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes)
        => _registry.TryGetNextPasses(out targetId, out passes);

    public void RenderScenePass(IScenePass pass, RenderPipeline submitter)
    {
        submitter.DrainCommandQueue(RenderTargetId.Scene);
    }

    public void RenderDepthPass(IDepthPass depthPass, RenderPipeline submitter)
    {
    }



    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation) =>
        _registry.MutateRenderPass(targetId, mutation);


    public void RegisterRenderTargetsFrom(in Vector2D<int> outputSize, RenderTargetDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.PostEffectTarget);
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Y, 16);

        desc.ScreenTarget.ScreenShaderId.IsValidOrThrow();
        
        _registry.SetOutputSize(outputSize);

        // Scene Target setup
        var sceneTarget = desc.SceneTarget;
        _registry.CreateSceneBuffer();
        _registry.CreateMultisampleBuffer(Vector2.One, sceneTarget.Samples);
        _registry.CreateLightBuffer(Vector2.One, TexturePreset.LinearMipmapRepeat);
        _registry.CreateShadowBuffer(new Vector2D<int>(2048, 2048));
        _registry.CreatePostProcessBuffer_A(Vector2.One);
        _registry.CreatePostProcessBuffer_B(Vector2.One);


        // Screen Target setup
        var screenTarget = desc.ScreenTarget;

        // Shadow passes


        // Scene Passes
        // Pass 0: draw scene into MSAA FBO
        _registry.RegisterRenderPass(RenderTargetId.Scene,
            new SceneRenderPass
            {
                TargetFbo = _registry.MultisampleFbo.FboId,
                Clear = new RenderPassClearDesc(Colors.CornflowerBlue, ClearBufferFlag.ColorAndDepth)
            });

        // Pass 1: resolve MSAA into single-sample texture FBO
        _registry.RegisterRenderPass(RenderTargetId.Scene,
            new BlitRenderPass
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
            Shader = desc.LightTarget.LightShaderId,
            Clear = new RenderPassClearDesc(desc.LightTarget.ClearColor, ClearBufferFlag.Color),
            Blend = desc.LightTarget.Blend,
        });

        // Post Processing Passes
        // Pass 0: Compose Scene + Light
        _registry.RegisterRenderPass(RenderTargetId.PostProcessing,
            new IfsqPass
            {
                TargetFbo = _registry.PostFboA.FboId,
                SourceTextures = [_registry.SceneFbo.ColTexId, _registry.LightFbo.ColTexId],
                Shader = desc.PostEffectTarget.CompositeShaderId
            });

        // Pass 1..N: post stack ping-pong (PostA <-> PostB)
        _registry.RegisterRenderPass(RenderTargetId.PostProcessing,
            new PostEffectPass
            {
                TargetFbo = _registry.PostFboB.FboId,
                SourceTextures = [_registry.PostFboA.ColTexId],
                Shader = desc.PostEffectTarget.EffectShaderId
            });


        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        _registry.RegisterRenderPass(RenderTargetId.Screen,
            new IfsqPass
            {
                TargetFbo = default,
                SourceTextures = [_registry.PostFboB.ColTexId],
                Shader = screenTarget.ScreenShaderId
            });
    }
}