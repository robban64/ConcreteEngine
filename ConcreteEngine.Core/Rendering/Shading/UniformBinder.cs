using System.Runtime.CompilerServices;
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


    private nuint _nextObjectCursor = 0;
    private nuint _capacity = 0;
    
    private int _drawObjectSize = 0;
    private int _drawObjectOffset = 0;

    public UniformBinder(IGraphicsDevice graphics)
    {
        _graphics = graphics;
        _gfx = graphics.Gfx;
    }

    public void Initialize()
    {
        _uboFrame = _graphics.GetUboIdBySlot(UniformGpuSlot.Frame);
        _uboView = _graphics.GetUboIdBySlot(UniformGpuSlot.Camera);
        _uboLight = _graphics.GetUboIdBySlot(UniformGpuSlot.DirLight);
        _uboMaterial = _graphics.GetUboIdBySlot(UniformGpuSlot.Material);
        _uboDraw = _graphics.GetUboIdBySlot(UniformGpuSlot.DrawObject);
        _uboFrame.IsValidOrThrow();
        _uboView.IsValidOrThrow();
        _uboLight.IsValidOrThrow();
        _uboMaterial.IsValidOrThrow();
        _uboDraw.IsValidOrThrow();
    }

    public void Prepare(nuint capacity)
    {
        _nextObjectCursor = 0;
        _drawObjectOffset = 0;
        _drawObjectSize = 0;
        _capacity = capacity;
        _gfx.BindUniformBuffer(_uboDraw);
        _gfx.SetUniformBufferSize(UniformGpuSlot.DrawObject, _capacity);
        _gfx.BindUniformBuffer(default);
    }

    public void UploadFrame(in FrameUniformRecord rec)
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
        _gfx.UploadUniformGpuData(UniformGpuSlot.Frame, in data);
    }

    public void UploadCamera(in CameraUniformRecord rec)
    {
        var data = new CameraUniformGpuData(
            viewMat: in rec.ViewMat,
            projMat: in rec.ProjMat,
            projViewMat: in rec.ProjViewMat,
            cameraPos: rec.CameraPos
        );

        _gfx.BindUniformBuffer(_uboView);
        _gfx.UploadUniformGpuData(UniformGpuSlot.Camera, in data);
    }

    public void UploadDirLight(in DirLightUniformRecord rec)
    {
        var data = new DirLightUniformGpuData(
            direction: rec.Direction,
            diffuse: rec.Diffuse,
            specular: rec.Specular,
            intensity: rec.Intensity
        );

        _gfx.BindUniformBuffer(_uboLight);
        _gfx.UploadUniformGpuData(UniformGpuSlot.DirLight, in data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadMaterial(in MaterialUniformRecord rec)
    {
        var data = new MaterialUniformGpuData(
            color: rec.Color,
            shininess: rec.Shininess,
            specularStrength: rec.SpecularStrength,
            uvRepeat: rec.UvRepeat
        );

        _gfx.BindUniformBuffer(_uboMaterial);
        _gfx.UploadUniformGpuData(UniformGpuSlot.Material, in data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadDrawObject(in DrawTransformPayload payload)
    {
        TransformHelper.GetNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniformGpuData(
            model: in payload.Transform,
            normal: in normalModel
        );

        _gfx.BindUniformBuffer(_uboDraw);
        var (offset, size, next) = _gfx.UploadUniformGpuDataRing(in data, _nextObjectCursor);
        _drawObjectSize = size;
        _nextObjectCursor = next;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindDrawObject()
    {
        _gfx.BindUniformBuffer(_uboDraw);
        _gfx.BindUniformBufferRange(UniformGpuSlot.DrawObject, _drawObjectOffset, _drawObjectSize);
        _drawObjectOffset += _drawObjectSize;
    }

}