#region

using System.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

#endregion

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawUniforms
{
    private readonly Camera3D _camera;
    private readonly GfxBuffers _gfxBuffers;

    private readonly RenderUbo _frameUbo;
    private readonly RenderUbo _cameraUbo;
    private readonly RenderUbo _dirLightUbo;
    private readonly RenderUbo _postUbo;

    public DrawUniforms(Camera3D camera, GfxBuffers gfxBuffers, RenderRegistry registry)
    {
        _camera = camera;
        _gfxBuffers = gfxBuffers;

        _frameUbo = registry.GetRenderUbo<FrameUniformRecord>();
        _cameraUbo = registry.GetRenderUbo<CameraUniformRecord>();
        _dirLightUbo = registry.GetRenderUbo<DirLightUniformRecord>();
        _postUbo = registry.GetRenderUbo<FramePostProcessUniform>();
    }

    private float _deltaTicker = 0;

    public void UploadGlobalUniforms(float alpha, in GfxFrameInfo frameCtx, in RenderGlobalSnapshot snapshot)
    {
        _deltaTicker += frameCtx.DeltaTime;
        UploadFrameUniformRecord(in snapshot);
        UploadDirLight(in snapshot);
        UploadCamera();
        UploadPost();
    }

    private void UploadFrameUniformRecord(in RenderGlobalSnapshot snapshot)
    {
        var data = new FrameUniformRecord(
            ambient: snapshot.Ambient,
            ambientIntensity: 1,
            fogColor: Vector3.One,
            fogDensity: 1,
            fogNear: 1,
            fogFar: 1,
            fogType: 1
        );
        _gfxBuffers.UploadUniformGpuData(_frameUbo.Id, in data, 0);
    }

    private void UploadDirLight(in RenderGlobalSnapshot snapshot)
    {
        var data = new DirLightUniformRecord(
            direction: snapshot.DirLight.Direction,
            diffuse: snapshot.DirLight.Diffuse,
            specular: snapshot.DirLight.Specular,
            intensity: snapshot.DirLight.Intensity
        );

        _gfxBuffers.UploadUniformGpuData(_dirLightUbo.Id, in data, 0);
    }

    private void UploadCamera()
    {
        var data = new CameraUniformRecord(
            viewMat: _camera.ViewMatrix,
            projMat: _camera.ProjectionMatrix,
            projViewMat: _camera.ProjectionViewMatrix,
            cameraPos: _camera.Translation
        );

        _gfxBuffers.UploadUniformGpuData(_cameraUbo.Id, in data, 0);
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

        _gfxBuffers.UploadUniformGpuData(_postUbo.Id, in data, 0);
    }
}