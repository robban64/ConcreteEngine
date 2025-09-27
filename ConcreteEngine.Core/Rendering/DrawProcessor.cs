using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawProcessor
{
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly GfxShaders _gfxShaders;

    private readonly IGfxResourceRepository _repository;
    
    private readonly MaterialStore _materials;

    private int _previousMaterialId = -1;

    private UboArena? _drawRing = null;

    internal DrawProcessor(GfxContext gfx, MaterialStore materials)
    {
        _gfx = gfx;
        _gfxCmd = gfx.Commands;
        _gfxBuffers = gfx.Buffers;
        _gfxShaders = gfx.Shaders;
        
        _repository = gfx.ResourceContext.Repository;
        _materials = materials;
    }


    public void Initialize()
    {
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals, nint capacity)
    {
        _drawRing = _repository.ShaderRepository.GetOrCreateUboArena(UniformGpuSlot.DrawObject);
        _drawRing.Prepare(capacity);
        
        _previousMaterialId = -1;
        _gfxBuffers.SetUniformBufferCapacity(UniformGpuSlot.DrawObject, capacity);
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

        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.Frame, in data, 0);
    }
    
    public void UploadFramePostProcess(in FramePostProcessUniform data)
    {
        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.PostProcess, in data, 0);
    }

    public void UploadCamera(in CameraUniformRecord rec)
    {
        var data = new CameraUniformGpuData(
            viewMat: in rec.ViewMat,
            projMat: in rec.ProjMat,
            projViewMat: in rec.ProjViewMat,
            cameraPos: rec.CameraPos
        );

        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.Camera, in data, 0);
    }

    public void UploadDirLight(in DirLightUniformRecord rec)
    {
        var data = new DirLightUniformGpuData(
            direction: rec.Direction,
            diffuse: rec.Diffuse,
            specular: rec.Specular,
            intensity: rec.Intensity
        );

        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.DirLight, in data, 0);

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

        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.Material, in data, 0);
    }

    private void BindMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        _gfxCmd.UseShader(material.ShaderId);
        for (int i = 0; i < material.SamplerSlots.Length; i++)
        {
            _gfxCmd.BindTexture(material.SamplerSlots[i], i);
        }

        UploadMaterial(new MaterialUniformRecord(materialId, material.Color.AsVec3(), material.Shininess,
            material.SpecularStrength, material.UvRepeat));

        _previousMaterialId = materialId.Id;
    }

    //TODO bulk upload
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadTransform(in DrawTransformPayload payload)
    {
        TransformUtils.CreateNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniformGpuData(
            model: in payload.Transform,
            normal: in normalModel
        );

        _gfxBuffers.UploadUniformGpuData(UniformGpuSlot.DrawObject, in data, _drawRing!.NextUploadCursor());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject()
    {
        _gfxBuffers.BindUniformBufferRange(UniformGpuSlot.DrawObject, _drawRing!.NextDrawCursor(), _drawRing.BlockSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawMesh(in DrawCommand cmd)
    {
        BindMaterial(cmd.MaterialId);
        BindDrawObject();
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
    

    public void DrawFullscreenQuad(IFsqPass pass)
    {
        _gfxCmd.UseShader(pass.Shader);
        //_gfxCmd.SetUniform(ShaderUniform.TexelSize, viewport.ConvertToVec2() * pass.SizeRatio);

        for (int i = 0; i < pass.SourceTextures.Length; i++)
        {
            _gfxCmd.BindTexture(pass.SourceTextures[i], i);
        }
        
        if (pass is PostEffectPass postEffectPass && postEffectPass.LutTexture != default)
        {
            //_gfxCmd.BindTexture(postEffectPass.LutTexture, pass.SourceTextures.Length);
        }

        _gfxCmd.BindMesh(_gfx.Primitives.FsqQuad);
        _gfxCmd.DrawBoundMesh(_gfx.Primitives.FsqQuad, 0);
    }
}