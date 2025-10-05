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

    private readonly UniformBufferId _frameUbo;
    private readonly UniformBufferId _cameraUbo;
    private readonly UniformBufferId _lightUbo;
    private readonly UniformBufferId _shadowUbo;
    private readonly UniformBufferId _dirLightUbo;
    private readonly UniformBufferId _postUbo;

    private float _deltaTicker = 0;

    public DrawUniforms(GfxBuffers gfxBuffers, RenderRegistry registry)
    {
        _gfxBuffers = gfxBuffers;

        _frameUbo = registry.GetRenderUbo<FrameUniformRecord>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUniformRecord>().Id;
        _dirLightUbo = registry.GetRenderUbo<DirLightUniformRecord>().Id;
        _lightUbo = registry.GetRenderUbo<LightUniformRecord>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUniformRecord>().Id;
        _postUbo = registry.GetRenderUbo<FramePostProcessUniform>().Id;
    }


    public void UploadGlobalUniforms(float alpha, in GfxFrameInfo frameCtx, RenderGlobalSnapshot snapshot)
    {
        _deltaTicker += frameCtx.DeltaTime;
        UploadFrameUniformRecord(snapshot);
        UploadDirLight(snapshot);
        UploadLight(snapshot);
        UploadPost();
    }

    public void UploadCameraView(RenderView view)
    {
        var data = new CameraUniformRecord(
            viewMat: in view.ViewMatrix,
            projMat: in  view.ProjectionMatrix,
            projViewMat: in  view.ProjectionViewMatrix,
            cameraPos: view.ViewPosition
        );

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
    }


    private void UploadFrameUniformRecord(RenderGlobalSnapshot snapshot)
    {
        /*
        var data = new FrameUniformRecord(
            ambient: new Vector4(snapshot.Ambient, 1),
            ambientGround: new Vector4(snapshot.Ambient, 1),
            fogColor: new Vector4(0.6f, 0.7f, 0.8f, 0.5f),
            fogParams0: new Vector4(0.00015f, 0.002f, 0.0f, 1.0f),
            fogParams1: new Vector4(1.0f, 1.0f, 200.0f, 0.0f)
        );
*/
        var data = new FrameUniformRecord(
            ambient: new Vector4(0.032f, 0.032f, 0.034f, 0.0f),
            ambientGround: new Vector4(0.015f, 0.015f, 0.013f, 0.0f),
            fogColor: new Vector4(0.76f, 0.81f, 0.86f, 0.055f),
            fogParams0: new Vector4(0.0000035f, 0.000090f, 0.0f, 0.15f),
            fogParams1: new Vector4(1.0f, 0.25f, 8000.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_frameUbo, in data, 0);
    }

    private void UploadDirLight(RenderGlobalSnapshot snapshot)
    {
        var data = new DirLightUniformRecord(
            direction: snapshot.DirLight.Direction.AsVector4(),
            diffuse: snapshot.DirLight.Diffuse,
            specular: new Vector4(snapshot.DirLight.SpecularIntensity, 0, 0, 0)
        );

/*
        Vector3 sunDir = Vector3.Normalize(new Vector3(0.35f, -1.0f, 0.2f));
        //var direction = new Vector4(Vector3.Normalize(snapshot.DirLight.Direction), 0.0f);
        var data = new DirLightUniformRecord(
            direction: sunDir.AsVector4(),
            diffuse: new Vector4(1.00f, 0.96f, 0.90f, 1.8f),
            specular: new Vector4(0.6f, 0.0f, 0.0f, 0.0f)
        );
*/
        _gfxBuffers.UploadUniformGpuData(_dirLightUbo, in data, 0);
    }

    private void UploadLight(RenderGlobalSnapshot snapshot)
    {
        var data = new LightUniformRecord(0, default);

        _gfxBuffers.UploadUniformGpuData(_lightUbo, in data, 0);
    }

    public void UploadShadow(in Matrix4x4 lightViewProjection)
    {
        var data = new ShadowUniformRecord(
            lightViewProj: lightViewProjection,
            shadowParams0: new Vector4(1.0f / 1024.0f, 1.0f / 1024.0f, 0.001f, 0.005f),
            shadowParams1: new Vector4(1.0f, 1.0f, 0.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
        var data = new FramePostProcessUniform(
            colorAdjust: new Vector4(0.10f, 1.05f, 1.03f, 2.20f),
            whiteBalance: new Vector4(0.05f, 0.00f, 0.04f, 0.00f),
            flags: new Vector4(1.0f, 0.0f, 0.18f, 0.00f),
            bloomParams: new Vector4(1.20f, 0.60f, 0.00f, 0.00f),
            bloomLods: new Vector4(0.70f, 0.40f, 0.20f, 0.10f),
            lutParams: new Vector4(0.00f, 0.00f, 0.00f, 0.00f),
            vignetteParams: new Vector4(0.78f, 0.96f, 0.08f, 0.00f),
            grainParams: new Vector4(0.00f, _deltaTicker, 0.00f, 0.00f),
            chromAbParams: new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
            toneShadows: new Vector4(208.0f, 0.02f, -0.005f, 0.12f),
            toneHighlights: new Vector4(45.0f, 0.02f, 0.004f, 0.12f),
            sharpenParams: new Vector4(0.06f, 1.5f, 0.02f, 0.00f)
        );

/*
      var data = new FramePostProcessUniform(
          flags: new Vector4(0, 0, 0, 0),
          colorAdjust: new Vector4(0, 1, 1, 1),
          whiteBalance: new Vector4(0, 0, 0, 0),
          bloomParams: new Vector4(0, 0, 0, 0),
          bloomLods: new Vector4(0, 0, 0, 0),
          toneShadows: new Vector4(0, 0, 0, 0),
          toneHighlights: new Vector4(0, 0, 0, 0),
          vignetteParams: new Vector4(0, 0, 0, 0),
          grainParams: new Vector4(0, 0, 0, 0),
          chromAbParams: new Vector4(0, 0, 0, 0),
          sharpenParams: new Vector4(0, 0, 0, 0),
          lutParams: new Vector4(0, 0, 0, 0)
      );
*/
        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}