using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class UniformBinder
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;

    private UniformBufferId _uboFrame;
    private UniformBufferId _uboView;
    private UniformBufferId _uboLight;
    private UniformBufferId _uboMaterial;
    private UniformBufferId _uboDraw;


    public UniformBinder(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
    }

    public void Initialize()
    {
        _uboFrame = _graphics.GetUboIdBySlot(ShaderBufferUniform.Frame);
        _uboView = _graphics.GetUboIdBySlot(ShaderBufferUniform.Camera);
        _uboLight = _graphics.GetUboIdBySlot(ShaderBufferUniform.DirLight);
        _uboMaterial = _graphics.GetUboIdBySlot(ShaderBufferUniform.Material);
        _uboDraw = _graphics.GetUboIdBySlot(ShaderBufferUniform.DrawObject);
        _uboFrame.IsValidOrThrow();
        _uboView.IsValidOrThrow();
        _uboLight.IsValidOrThrow();
        _uboMaterial.IsValidOrThrow();
        _uboDraw.IsValidOrThrow();
    }

    public void ApplyFrame(in FrameUniformRecord rec)
    {
        var data = new FrameUniformGpuData(
            ambient: rec.Ambient,
            ambientIntensity: rec.AmbientIntensity,
            fogColor: rec.FogColor,
            fogDensity: rec.FogDensity,
            fogNear: rec.FogNear,
            fogFar: rec.FogFar,
            fogType: rec.FogType
        );

        _gfx.BindUniformBuffer(_uboFrame);
        _gfx.UploadUniformGpuData(ShaderBufferUniform.Frame, in data);
    }

    public void ApplyCamera(in CameraUniformRecord rec)
    {
        var data = new CameraUniformGpuData(
            viewMat: in rec.ViewMat,
            projMat: in rec.ProjMat,
            projViewMat: in rec.ProjViewMat,
            cameraPos: rec.CameraPos
        );

        _gfx.BindUniformBuffer(_uboView);
        _gfx.UploadUniformGpuData(ShaderBufferUniform.Camera, in data);
    }

    public void ApplyDirLight(in DirLightUniformRecord rec)
    {
        var data = new DirLightUniformGpuData(
            direction: rec.Direction,
            diffuse: rec.Diffuse,
            specular: rec.Specular,
            intensity: rec.Intensity
        );

        _gfx.BindUniformBuffer(_uboLight);
        _gfx.UploadUniformGpuData(ShaderBufferUniform.DirLight, in data);
    }

    public void ApplyMaterial(in MaterialUniformRecord rec)
    {
        var data = new MaterialUniformGpuData(
            color: rec.Color,
            shininess: rec.Shininess,
            specularStrength: rec.SpecularStrength,
            uvRepeat: rec.UvRepeat
        );

        _gfx.BindUniformBuffer(_uboMaterial);
        _gfx.UploadUniformGpuData(ShaderBufferUniform.Material, in data);
    }

    public void ApplyDrawObject(in DrawObjectUniformRecord rec)
    {
        var data = new DrawObjectUniformGpuData(
            model: in rec.Model,
            normal: in rec.NormalModel
        );

        _gfx.BindUniformBuffer(_uboDraw);
        _gfx.UploadUniformGpuData(ShaderBufferUniform.DrawObject, in data);
    }
}