using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Core.Rendering.Utility;
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

    private readonly RenderRegistry _registry;

    private readonly MaterialStore _materials;

    private int _previousMaterialId = -1;

    private UboArena? _drawRing = null;
    private RenderUbo _drawUbo = null!;
    
    private RenderUbo FrameUbo => _registry.GetRenderUbo<FrameUniformGpuData>();
    private RenderUbo CameraUbo => _registry.GetRenderUbo<CameraUniformGpuData>();
    private RenderUbo DirLightUbo => _registry.GetRenderUbo<DirLightUniformGpuData>();
    private RenderUbo MaterialUbo => _registry.GetRenderUbo<MaterialUniformGpuData>();

    private RenderUbo PostUbo => _registry.GetRenderUbo<FramePostProcessUniform>();

    internal DrawProcessor(GfxContext gfx, MaterialStore materials)
    {
        _gfx = gfx;
        _gfxCmd = gfx.Commands;
        _gfxBuffers = gfx.Buffers;
        _gfxShaders = gfx.Shaders;
        
        _materials = materials;
    }


    public void Initialize()
    {
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals, nint capacity)
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniformGpuData>();
        _drawRing = _drawUbo.UboArena();
        _drawRing.Prepare(capacity);
        
        _previousMaterialId = -1;
        _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, capacity);
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

        
        _gfxBuffers.UploadUniformGpuData(FrameUbo.Id, in data, 0);
    }
    
    public void UploadFramePostProcess(in FramePostProcessUniform data)
    {
        _gfxBuffers.UploadUniformGpuData(PostUbo.Id, in data, 0);
    }

    public void UploadCamera(in CameraUniformRecord rec)
    {
        var data = new CameraUniformGpuData(
            viewMat: in rec.ViewMat,
            projMat: in rec.ProjMat,
            projViewMat: in rec.ProjViewMat,
            cameraPos: rec.CameraPos
        );

        _gfxBuffers.UploadUniformGpuData(CameraUbo.Id, in data, 0);
    }

    public void UploadDirLight(in DirLightUniformRecord rec)
    {
        var data = new DirLightUniformGpuData(
            direction: rec.Direction,
            diffuse: rec.Diffuse,
            specular: rec.Specular,
            intensity: rec.Intensity
        );

        _gfxBuffers.UploadUniformGpuData(DirLightUbo.Id, in data, 0);

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

        _gfxBuffers.UploadUniformGpuData(MaterialUbo.Id, in data, 0);
    }

    private void UseShader(ShaderId shaderId)
    {
        var renderShader = _registry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId, renderShader.Locations);

    }

    private void BindMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        UseShader(material.ShaderId);
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

        _gfxBuffers.UploadUniformGpuData(_drawUbo.Id, in data, _drawRing!.NextUploadCursor());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject()
    {
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, _drawRing!.NextDrawCursor(), _drawRing.BlockSize);
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
        UseShader(pass.Shader);
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