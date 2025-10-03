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
    private readonly Camera3D _camera;
    private readonly GfxBuffers _gfxBuffers;

    private readonly UniformBufferId _frameUbo;
    private readonly UniformBufferId _cameraUbo;
    private readonly UniformBufferId _lightUbo;
    private readonly UniformBufferId _shadowUbo;
    private readonly UniformBufferId _dirLightUbo;
    private readonly UniformBufferId _postUbo;

    private float _deltaTicker = 0;

    public DrawUniforms(Camera3D camera, GfxBuffers gfxBuffers, RenderRegistry registry)
    {
        _camera = camera;
        _gfxBuffers = gfxBuffers;

        _frameUbo = registry.GetRenderUbo<FrameUniformRecord>().Id;
        _cameraUbo = registry.GetRenderUbo<CameraUniformRecord>().Id;
        _dirLightUbo = registry.GetRenderUbo<DirLightUniformRecord>().Id;
        _lightUbo = registry.GetRenderUbo<LightUniformRecord>().Id;
        _shadowUbo = registry.GetRenderUbo<ShadowUniformRecord>().Id;
        _postUbo = registry.GetRenderUbo<FramePostProcessUniform>().Id;
    }


    public void UploadGlobalUniforms(float alpha, in GfxFrameInfo frameCtx, in RenderGlobalSnapshot snapshot)
    {
        _deltaTicker += frameCtx.DeltaTime;
        UploadFrameUniformRecord(snapshot);
        UploadDirLight(snapshot);
        UploadLight(snapshot);
        UploadShadow(snapshot);
        UploadCamera();
        UploadPost();
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
            ambient: new Vector4(0.030f, 0.030f, 0.032f, 0.0f),
            ambientGround: new Vector4(0.012f, 0.012f, 0.011f, 0.0f),
            fogColor: new Vector4(0.70f, 0.74f, 0.78f, 0.08f),
            fogParams0: new Vector4(0.000006f, 0.00012f, 0.0f, 0.20f),
            fogParams1: new Vector4(1.0f, 0.25f, 6000.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_frameUbo, in data, 0);
    }

    private void UploadCamera()
    {
        var data = new CameraUniformRecord(
            viewMat: _camera.ViewMatrix,
            projMat: _camera.ProjectionMatrix,
            projViewMat: _camera.ProjectionViewMatrix,
            cameraPos: _camera.Translation
        );

        _gfxBuffers.UploadUniformGpuData(_cameraUbo, in data, 0);
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

    private void UploadShadow(RenderGlobalSnapshot snapshot)
    {
        var data = new ShadowUniformRecord(
            lightViewProj: Matrix4x4.Identity,
            shadowParams0: new Vector4(1.0f / 1024.0f, 1.0f / 1024.0f, 0.001f, 0.005f),
            shadowParams1: new Vector4(0, 1.0f, 0.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_shadowUbo, in data, 0);
    }

    private void UploadPost()
    {
      
        var data = new FramePostProcessUniform(
            colorAdjust: new Vector4(0.25f, 1.15f, 1.10f, 2.2f),
            whiteBalance: new Vector4(0.15f, 0.02f, 0.10f, 0.0f),
            flags: new Vector4(1.0f, 0.0000f, 0.6f, 0.6f),
            bloomParams: new Vector4(0.70f, 0.65f, 0.0f, 0.0f),
            bloomLods: new Vector4(0.8f, 0.55f, 0.30f, 0.15f),
            lutParams: new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
            vignetteParams: new Vector4(0.32f, 0.88f, 0.25f, 0.0f),
            grainParams: new Vector4(0, _deltaTicker, 0.0f, 0.0f),
            chromAbParams: new Vector4(0.0000f, 0.0f, 0.0f, 0.0f),
            toneShadows: new Vector4(210.0f, 0.05f, -0.01f, 0.4f),
            toneHighlights: new Vector4(40.0f, 0.05f, 0.01f, 0.4f),
            sharpenParams: new Vector4(0.10f, 1.5f, 0.05f, 0.0f)
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