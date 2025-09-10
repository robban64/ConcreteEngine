using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawProcessor
{
    private readonly IGraphicsDevice _graphics;
    private readonly IGraphicsContext _gfx;
    private readonly MaterialStore _materials;

    private int _previousMaterialId = -1;

    private UboArena? _drawRing = null;

    internal DrawProcessor(IGraphicsDevice graphics, MaterialStore materials)
    {
        _graphics = graphics;
        _materials = materials;
        _gfx = _graphics.Gfx;
    }


    public void Initialize()
    {
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals, nuint capacity)
    {
        _drawRing = _graphics.ShaderRegistry.GetOrCreateUboArena(UniformGpuSlot.DrawObject);
        _drawRing.Prepare(capacity);
        
        _previousMaterialId = -1;
        _gfx.BindUniformBuffer(UniformGpuSlot.DrawObject);
        _gfx.SetUniformBufferSize(UniformGpuSlot.DrawObject, capacity);
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

        _gfx.BindUniformBuffer(UniformGpuSlot.Frame);
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

        _gfx.BindUniformBuffer(UniformGpuSlot.Camera);
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

        _gfx.BindUniformBuffer(UniformGpuSlot.DirLight);
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

        _gfx.BindUniformBuffer(UniformGpuSlot.Material);
        _gfx.UploadUniformGpuData(UniformGpuSlot.Material, in data);
    }

    private void BindMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        _gfx.UseShader(material.ShaderId);
        for (int t = 0; t < material.SamplerSlots.Length; t++)
        {
            _gfx.BindTexture(material.SamplerSlots[t], (uint)t);
        }

        UploadMaterial(new MaterialUniformRecord(materialId, material.Color.AsVec3(), material.Shininess,
            material.SpecularStrength, material.UvRepeat));

        _previousMaterialId = materialId.Id;
    }

    //TODO bulk upload
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadTransform(in DrawTransformPayload payload)
    {
        TransformHelper.GetNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniformGpuData(
            model: in payload.Transform,
            normal: in normalModel
        );

        _gfx.BindUniformBuffer(UniformGpuSlot.DrawObject);
        _gfx.UploadUniformGpuData(UniformGpuSlot.DrawObject, in data, _drawRing!.NextUploadCursor());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject()
    {
        _gfx.BindUniformBuffer(UniformGpuSlot.DrawObject);
        _gfx.BindUniformBufferRange(UniformGpuSlot.DrawObject, _drawRing!.NextDrawCursor(), _drawRing.BlockSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(in DrawCommand cmd)
    {
        BindMaterial(cmd.MaterialId);
        BindDrawObject();
        _gfx.BindMesh(cmd.MeshId);
        _gfx.DrawMesh(cmd.DrawCount);
    }
    

}