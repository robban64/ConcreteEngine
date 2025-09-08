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
        _capacity = capacity;
        _gfx.BindUniformBuffer(_uboDraw);
        _gfx.SetUniformBufferSize(UniformGpuSlot.DrawObject, _capacity);
        _gfx.BindUniformBuffer(default);
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
        _gfx.UploadUniformGpuData(UniformGpuSlot.Frame, in data);
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
        _gfx.UploadUniformGpuData(UniformGpuSlot.Camera, in data);
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
        _gfx.UploadUniformGpuData(UniformGpuSlot.DirLight, in data);
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
        _gfx.UploadUniformGpuData(UniformGpuSlot.Material, in data);
    }

    public void ApplyDrawObject(in DrawObjectUniformRecord rec)
    {
        TransformHelper.GetNormalMatrix(in rec.Model, out var normalModel);

        var data = new DrawObjectUniformGpuData(
            model: in rec.Model,
            normal: in normalModel
        );

        _gfx.BindUniformBuffer(_uboDraw);
        var (offset, size, next) = _gfx.UploadUniformGpuDataRing(in data, _nextObjectCursor);
        _gfx.BindUniformBufferRange(UniformGpuSlot.DrawObject, offset, size);
        _nextObjectCursor = next;
        //_gfx.UploadUniformGpuData(UniformGpuData.DrawObject, in data);
        


    }

    private sealed class UboStream
    {
        public readonly uint BufferId;
        public readonly nuint Capacity;
        public readonly nuint Align; // GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT
        private nuint _cursor;

        public void BeginFrame()
        {
            _cursor = 0;
        }

        public (nuint offset, nuint size) Allocate(nuint size)
        {
            nuint aligned = AlignUp(_cursor, Align);
            if (aligned + size > Capacity)
            {
                aligned = 0;
                _cursor = 0;
            }
            _cursor = aligned + size;
            return (aligned, size);
        }

        static nuint AlignUp(nuint v, nuint a) => a == 0 ? v : (v + (a - 1)) & ~(a - 1);
    }
}