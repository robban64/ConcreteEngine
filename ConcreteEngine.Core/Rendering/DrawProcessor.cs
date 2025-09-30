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
    
    private RenderUbo MaterialUbo => _registry.GetRenderUbo<MaterialUniformRecord>();

    internal DrawProcessor(GfxContext gfx, MaterialStore materials, RenderRegistry registry)
    {
        _gfx = gfx;
        _gfxCmd = gfx.Commands;
        _gfxBuffers = gfx.Buffers;
        _gfxShaders = gfx.Shaders;
        
        _materials = materials;
        _registry = registry;
    }


    public void Initialize()
    {
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals, nint capacity)
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _drawRing = _drawUbo.UboArena();
        _drawRing.Prepare(capacity);
        
        _previousMaterialId = -1;
        _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, capacity);
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

        UploadMaterial(new MaterialUniformRecord(material.Color.AsVec3(), material.Shininess,
            material.SpecularStrength, material.UvRepeat));

        _previousMaterialId = materialId.Id;
    }
    
    public void UploadMaterial(in MaterialUniformRecord rec)
    {
        var data = new MaterialUniformRecord(
            color: rec.Color,
            shininess: rec.Shininess,
            specularStrength: rec.SpecularStrength,
            uvRepeat: rec.UvRepeat
        );

        _gfxBuffers.UploadUniformGpuData(MaterialUbo.Id, in data, 0);
    }

    //TODO bulk upload
    public void UploadTransform(in DrawTransformPayload payload)
    {
        TransformUtils.CreateNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniform(
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

    public void DrawMesh(in DrawCommand cmd)
    {
        BindMaterial(cmd.MaterialId);
        BindDrawObject();
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
    

    public void DrawFullscreenQuad(ShaderId shaderId, IReadOnlyList<TextureId> sources)
    {
        UseShader(shaderId);

        for (int i = 0; i < sources.Count; i++)
        {
            _gfxCmd.BindTexture(sources[i], i);
        }
        
        _gfxCmd.BindMesh(_gfx.Primitives.FsqQuad);
        _gfxCmd.DrawBoundMesh(_gfx.Primitives.FsqQuad, 0);
    }
    
    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        UseShader(shaderId);

        for (int i = 0; i < sources.Length; i++)
        {
            _gfxCmd.BindTexture(sources[i], i);
        }
        
        _gfxCmd.BindMesh(_gfx.Primitives.FsqQuad);
        _gfxCmd.DrawBoundMesh(_gfx.Primitives.FsqQuad, 0);
    }
}