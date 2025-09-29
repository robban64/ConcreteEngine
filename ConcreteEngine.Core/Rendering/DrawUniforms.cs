using System.Numerics;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawUniforms
{
    private readonly Camera3D _camera;
    private readonly DrawProcessor _drawProcessor;

    private float _deltaTicker = 0;

    public void UploadFrameUniforms(float alpha, in FrameInfo frameCtx, in RenderGlobalSnapshot snapshot)
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
            colorAdjust: new Vector4(0.25f, 1.15f, 1.10f, 2.2f),
            whiteBalance: new Vector4(0.15f, 0.02f, 0.10f, 0.0f),
            flags: new Vector4(1.0f, 0.0000f, 0.6f, 0.6f),
            bloomParams: new Vector4(0.70f, 0.65f, 0.0f, 0.0f),
            bloomLods: new Vector4(0.8f, 0.55f, 0.30f, 0.15f),
            lutParams: new Vector4(0.0f, 0.0f, 0.0f, 0.0f),
            vignetteParams: new Vector4(0.32f, 0.88f, 0.25f, 0.0f),
            grainParams: new Vector4(0.008f, _deltaTicker, 0.0f, 0.0f),
            chromAbParams: new Vector4(0.0000f, 0.0f, 0.0f, 0.0f),
            toneShadows: new Vector4(210.0f, 0.05f, -0.01f, 0.4f),
            toneHighlights: new Vector4(40.0f, 0.05f, 0.01f, 0.4f),
            sharpenParams: new Vector4(0.10f, 1.5f, 0.05f, 0.0f)
        );

        _drawProcessor.UploadFramePostProcess(data: in postProcessUniforms);
    }
}