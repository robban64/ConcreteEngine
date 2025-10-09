#region

using System.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class DrawUniforms
{
    private readonly GfxBuffers _gfxBuffers;

    private readonly UniformBufferId _engineUbo;
    private readonly UniformBufferId _frameUbo;
    private readonly UniformBufferId _cameraUbo;
    private readonly UniformBufferId _lightUbo;
    private readonly UniformBufferId _shadowUbo;
    private readonly UniformBufferId _dirLightUbo;
    private readonly UniformBufferId _postUbo;

    private readonly RenderSceneState _sceneState;

    internal DrawUniforms(GfxBuffers gfxBuffers, RenderRegistry registry, RenderSceneState sceneState)
    {
        _gfxBuffers = gfxBuffers;
        _sceneState = sceneState;

        _engineUbo = registry.GetRenderUbo<EngineUniformRecord>().Id;
        _frameUbo = registry.GetRenderUbo<FrameUniformRecord>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUniformRecord>().Id;
        _dirLightUbo = registry.GetRenderUbo<DirLightUniformRecord>().Id;
        _lightUbo = registry.GetRenderUbo<LightUniformRecord>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUniformRecord>().Id;
        _postUbo = registry.GetRenderUbo<PostProcessUniform>().Id;
    }


    public void UploadGlobalUniforms(in RenderTickInfo tickInfo, in RenderTickParams tickParams)
    {
        UploadEngineUniformRecord(in tickInfo, in tickParams);
        UploadFrameUniformRecord();
        UploadDirLight();
        UploadLight();
        UploadPost();
    }

    public void UploadCameraView(RenderView view)
    {
        var data = new CameraUniformRecord(
            viewMat: in view.ViewMatrix,
            projMat: in view.ProjectionMatrix,
            projViewMat: in view.ProjectionViewMatrix,
            cameraPos: view.Position
        );

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
    }

    private void UploadEngineUniformRecord(in RenderTickInfo tickInfo, in RenderTickParams tickParams)
    {
        var outputSize = tickInfo.OutputSize;
        var data = new EngineUniformRecord(
            deltaTime: tickInfo.DeltaTime,
            invResolution: new Vector2(1.0f / outputSize.Width, 1.0f / outputSize.Height),
            time: tickParams.Time,
            mouse: tickParams.MousePos,
            random: tickParams.RndSeed
        );

        _gfxBuffers.UploadUniformGpuData(_engineUbo, in data, 0);
    }

    private void UploadFrameUniformRecord()
    {
        /*
        var data = new FrameUniformRecord(
            ambient: new Vector4(snapshot.Ambient, 1),
            ambientGround: new Vector4(snapshot.Ambient, 1),
            fogColor: new Vector4(0.6f, 0.7f, 0.8f, 0.5f),
            fogParams0: new Vector4(0.00015f, 0.002f, 0.0f, 1.0f),
            fogParams1: new Vector4(1.0f, 1.0f, 200.0f, 0.0f)

            ambient: new Vector4(0.032f, 0.032f, 0.034f, 0.0f),
           ambientGround: new Vector4(0.015f, 0.019f, 0.013f, 0.0f),
           fogColor: new Vector4(0.76f, 0.81f, 0.86f, 0.055f),
           fogParams0: new Vector4(0.0000035f, 0.000090f, 0.0f, 0.15f),
           fogParams1: new Vector4(1.0f, 0.25f, 8000.0f, 0.0f)

        );
*/
        var fog = _sceneState.Fog;
        var ambient = _sceneState.Ambient;

        float kExp2 = 1f / (fog.Density * fog.Density);
        float kHeight = 1f / MathF.Max(fog.HeightFalloff, 1e-6f);

        var data = new FrameUniformRecord(
            ambient: new Vector4(ambient.Ambient, ambient.Exposure),// xyz = sky ambient, w = exposure
            ambientGround: new Vector4(ambient.AmbientGround, 0.0f),// xyz = ground ambient
            fogColor: new Vector4(fog.Color, fog.Scattering),// rgb = base fog color, a = in-scattering mix
            fogParams0: new Vector4(kExp2, kHeight, fog.BaseHeight, fog.Strength),// x=exp2_k, y=height_k, z=height0, w=globalStrength
            fogParams1: new Vector4(1f, fog.HeightInfluence, fog.MaxDistance, 0.0f)// x=expWeight, y=heightWeight, z=maxDistance, w=reserved
        );

        _gfxBuffers.UploadUniformGpuData(_frameUbo, in data, 0);
    }

    private void UploadDirLight()
    {
        var dirLight = _sceneState.DirLight;
        var data = new DirLightUniformRecord(
            direction: dirLight.Direction.AsVector4(),
            diffuse: new Vector4(dirLight.Diffuse, dirLight.Intensity),
            specular: new Vector4(dirLight.Specular, 0.0f, 0.0f, 0.0f)
        );
        _gfxBuffers.UploadUniformGpuData(_dirLightUbo, in data, 0);
    }

    private void UploadLight()
    {
        var data = new LightUniformRecord(0, default);

        _gfxBuffers.UploadUniformGpuData(_lightUbo, in data, 0);
    }

    public void UploadShadow(in Matrix4x4 lightViewProjection)
    {
        //0.001f, 0.005f
        // 0.0004f, 0.0025f

        var shadow = _sceneState.Shadows;
        var size = 1.0f / shadow.ShadowMapSize;
        var data = new ShadowUniformRecord(
            lightViewProj: lightViewProjection,
            shadowParams0: new Vector4(size, size, shadow.ConstBias, shadow.SlopeBias),
            shadowParams1: new Vector4(shadow.Strength, shadow.PcfRadius, 0.03f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
        
        /*
        var data = new PostProcessUniform(
            grade: new Vector4(-0.015f, 1.10f, 0.96f, 0.018f),
            whiteBalance: new Vector4(-0.003f, 0.25f, 0.0f, 0.0f),
            bloom: new Vector4(0.55f, 0.78f, 1.10f, 0.0f),
            fx: new Vector4(0.04f, 0.0025f, 0.065f, 0.095f)
        );
*/
        var effect = _sceneState.PostEffects;
        var (g, wb, b, fx) = (effect.Grade, effect.WhiteBalance, effect.Bloom, Fx: effect.ImageFx);
        var data = new PostProcessUniform(
            grade: new Vector4(g.Exposure * 0.10f, 0.8f + g.Saturation * 0.4f, 0.9f + g.Contrast * 0.2f, g.Warmth * 0.05f),
            whiteBalance: new Vector4(wb.Tint * 0.05f, wb.Strength, 0f, 0f),
            bloom: new Vector4(b.Intensity * 1.5f, 0.6f + b.Threshold * 0.3f, b.Radius, 0f),
            fx: new Vector4(fx.Vignette * 0.15f, fx.Grain * 0.01f, fx.Sharpen * 0.15f, fx.Rolloff * 0.12f)
        );
        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}