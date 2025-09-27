using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

internal sealed class Render3D : IRender
{
    private readonly GfxContext _gfx;
    private readonly DrawProcessor _drawProcessor;

    private readonly RenderPasses _registry;
    private readonly Camera3D _camera;

    private float _deltaTicker = 0;

    public ICamera Camera => _camera;


    //private TextureId LutTextureId = default;


    public Render3D(GfxContext gfx, DrawProcessor drawProcessor, in RenderGlobalSnapshot snapshot)
    {
        _gfx = gfx;
        _drawProcessor = drawProcessor;
        _camera = new Camera3D();
        _registry = new RenderPasses(_gfx, in snapshot);
/*
        var texDesc = new GfxTextureDescriptor(32, 32, TextureKind.Texture3D,
            EnginePixelFormat.Rgb, 32);

        var texProps = new GfxTextureProperties(TexturePreset.LinearClamp, TextureAnisotropy.Off, 0);
        LutTextureId = _gfx.Textures.CreateTexture(in texDesc, in texProps);
        _gfx.Textures.ApplyProperties(LutTextureId);
        */
    }


    public void Prepare(float alpha, in FrameInfo frameCtx, in RenderGlobalSnapshot snapshot)
    {
        _deltaTicker += frameCtx.DeltaTime;

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

        _drawProcessor.UploadFrame(rec: in frameUniforms);
        _drawProcessor.UploadCamera(rec: in cameraUniforms);
        _drawProcessor.UploadDirLight(rec: in dirLightUniforms);

        var postProcessUniforms = new FramePostProcessUniform(
            colorAdjust:   new Vector4(0.0f,  1.05f, 1.05f, 2.2f),
            whiteBalance:  new Vector4(0.05f, 0.02f, 0.15f, 0.1f),
            flags:         new Vector4(1.0f,  0.001f, 0.6f,  0.6f),
            bloomParams:   new Vector4(0.7f,  0.6f,  0.0f,  0.0f),
            bloomLods:     new Vector4(0.9f,  0.6f,  0.35f, 0.2f),
            lutParams:     new Vector4(0.0f,  0.0f,  0.0f,  0.0f),
            vignetteParams: new Vector4(x: 0.35f, y: 0.85f, z: 0.20f, w: 0.0f),
            grainParams: new Vector4(0.0075f, _deltaTicker, 0.0f, 0.0f),
            chromAbParams: new Vector4(0.0000f, 0.0f, 0.0f, 0.0f)
        );

        _drawProcessor.UploadFramePostProcess(data: in postProcessUniforms);
    }

    public bool TryGetNextPasses(out RenderTargetId targetId, out List<IRenderPassDescriptor> passes) =>
        _registry.TryGetNextPasses(out targetId, out passes);

    public void RenderScenePass(IScenePass pass, RenderPipeline submitter)
    {
        submitter.DrainCommandQueue(RenderTargetId.Scene);
    }

    public void RenderDepthPass(IDepthPass depthPass, RenderPipeline submitter)
    {
    }

    public void RenderPostEffectPass(PostEffectPass pass)
    {
        _drawProcessor.DrawFullscreenQuad(pass);
    }

    public void RenderScreenPass(ScreenPass pass)
    {
        _gfx.Commands.ApplyState(_gfx.Commands.ActiveState with { FramebufferSrgb = false });
        _drawProcessor.DrawFullscreenQuad(pass);
    }


    public void MutateRenderPass(RenderTargetId targetId, in RenderPassMutation mutation) =>
        _registry.MutateRenderPass(targetId, mutation);


    public void RegisterRenderTargetsFrom(in Vector2D<int> outputSize, RenderTargetDescriptor desc)
    {
        ArgumentNullException.ThrowIfNull(desc);
        ArgumentNullException.ThrowIfNull(desc.SceneTarget);
        ArgumentNullException.ThrowIfNull(desc.ScreenTarget);
        //ArgumentNullException.ThrowIfNull(desc.LightTarget);
        ArgumentNullException.ThrowIfNull(desc.PostEffectTarget);
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(outputSize.Y, 16);

        desc.ScreenTarget.ScreenShaderId.IsValidOrThrow();

        // Scene Target setup
        var sceneTarget = desc.SceneTarget;
        _registry.CreateSceneBuffer();
        _registry.CreateMultisampleBuffer(Vector2.One, sceneTarget.Samples);
        //_registry.CreateLightBuffer(Vector2.One, TexturePreset.LinearMipmapRepeat);
        //_registry.CreateShadowBuffer(new Vector2D<int>(2048, 2048));
        _registry.CreatePostProcessBuffer_A();
        _registry.CreatePostProcessBuffer_B();


        // Screen Target setup
        var screenTarget = desc.ScreenTarget;

        // Shadow passes


        // Scene Passes
        // Pass 0: draw scene into MSAA FBO
        _registry.RegisterRenderPass(RenderTargetId.Scene,
            new SceneRenderPass
            {
                TargetFbo = _registry.MultisampleFbo.FboId,
                Clear = GfxPassClear.MakeColorDepthClear(desc.SceneTarget.ClearColor)
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
        /*
        _registry.RegisterRenderPass(RenderTargetId.Light, new LightRenderPass
        {
            TargetFbo = _registry.LightFbo.FboId,
            Shader = desc.LightTarget.LightShaderId,
            Clear = new RenderPassClearDesc(desc.LightTarget.ClearColor, ClearBufferFlag.Color),
            Blend = desc.LightTarget.Blend,
        });
        */
        // Post Processing Passes
        // Pass 0: Compose Scene + Light
        //                SourceTextures = [_registry.SceneFbo.ColTexId, _registry.LightFbo.ColTexId],

        _registry.RegisterRenderPass(RenderTargetId.PostProcessing,
            new PostEffectPass
            {
                TargetFbo = _registry.PostFboA.FboId,
                SourceTextures = [_registry.SceneFbo.Attachments.ColorTextureId],
                OutputTexture = _registry.PostFboA.Attachments.ColorTextureId,
                Shader = desc.PostEffectTarget.CompositeShaderId,
                GenerateMipMapAfter = true,
            });

        // Pass 1..N: post stack ping-pong (PostA <-> PostB)
        _registry.RegisterRenderPass(RenderTargetId.PostProcessing,
            new PostEffectPass
            {
                TargetFbo = _registry.PostFboB.FboId,
                SourceTextures = [_registry.PostFboA.Attachments.ColorTextureId],
                //LutTexture = LutTextureId,
                OutputTexture = _registry.PostFboB.Attachments.ColorTextureId,
                Shader = desc.PostEffectTarget.EffectShaderId,
            });


        // Screen Passes
        // Pass 0: Combine scene and light fbo texture into final scene
        _registry.RegisterRenderPass(RenderTargetId.Screen,
            new ScreenPass
            {
                TargetFbo = default,
                SourceTextures = [_registry.PostFboB.Attachments.ColorTextureId],
                Shader = screenTarget.ScreenShaderId
            });
    }
}