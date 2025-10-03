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
            ambient: new Vector4(0.035f, 0.035f, 0.038f, 0.0f),
            ambientGround: new Vector4(0.012f, 0.012f, 0.010f, 0.0f),
            fogColor: new Vector4(0.60f, 0.70f, 0.80f, 0.50f),
            fogParams0: new Vector4(0.00010f, 0.0015f, 0.0f, 1.0f),
            fogParams1: new Vector4(1.0f,     0.8f,     400.0f, 0.0f)
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
        /*
        var data = new DirLightUniformRecord(
            direction: snapshot.DirLight.Direction.AsVector4(),
            diffuse: new  Vector4(snapshot.DirLight.Diffuse, 1),
            specular: snapshot.DirLight.Specular.AsVector4()
        );
*/
        Vector3 sunDir = Vector3.Normalize(new Vector3(0.35f, -1.0f, 0.2f));
        var direction = new Vector4(sunDir, 0.0f);


        var data = new DirLightUniformRecord(
            direction: direction,
            diffuse: new Vector4(1.00f, 0.96f, 0.90f, 3.0f),
            specular: new Vector4(1.0f, 0.0f, 0.0f, 0.0f)
        );
        _gfxBuffers.UploadUniformGpuData(_dirLightUbo, in data, 0);
    }
    
    private void UploadLight(RenderGlobalSnapshot snapshot)
    {
        var data = new LightUniformRecord(1, default);

        _gfxBuffers.UploadUniformGpuData(_lightUbo, in data, 0);
    }

    private void UploadShadow(RenderGlobalSnapshot snapshot)
    {
        var data = new ShadowUniformRecord(
            lightViewProj: Matrix4x4.Identity, 
            shadowParams0: new Vector4(1.0f/1024.0f, 1.0f/1024.0f, 0.001f, 0.005f),
            shadowParams1: new Vector4(1.0f, 1.0f, 0.0f, 0.0f)
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

        _gfxBuffers.UploadUniformGpuData(_postUbo, in data, 0);
    }
}