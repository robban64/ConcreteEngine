#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

internal sealed class DrawProcessor
{
    private readonly GfxContext _gfx;
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly GfxShaders _gfxShaders;

    private readonly RenderRegistry _registry;

    private readonly MaterialStore _materials;

    private int _previousMaterialId = -1;

    private RenderUbo _drawUbo = null!;

    private UniformBufferId _materialUboId;

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
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _materialUboId = _registry.GetRenderUbo<MaterialUniformRecord>().Id;
    }

    public void Prepare(in RenderGlobalSnapshot renderGlobals, nint capacity)
    {
        _drawUbo.ResetCursor();
        if (capacity != _drawUbo.Capacity)
            _drawUbo.SetCapacity(capacity);

        _previousMaterialId = -1;
        _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, capacity);
    }

    private void UseShader(ShaderId shaderId)
    {
        var renderShader = _registry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId, renderShader.Locations);
    }

    private void BindDrawMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId.Id) return;
        var material = _materials.GetMaterial(materialId);
        UseShader(material.ShaderId);

        for (int i = 0; i < material.SamplerSlots.Length; i++)
        {
            var value = material.SamplerSlots[i];
            if(value == 0) continue;
            _gfxCmd.BindTexture(value, i);
        }

        UploadMaterial(material);

        _previousMaterialId = materialId.Id;
    }

    public void UploadMaterial(Material mat)
    {
        var data = new MaterialUniformRecord(
            matColor: new Vector4(mat.Color.AsVec3(), 1),
            matParams0: new Vector4(mat.SpecularStrength, mat.UvRepeat, 0.0f, 0.0f),
            matParams1: new Vector4(mat.Shininess, mat.HasNormalMap?1.0f:0.0f, 0.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_materialUboId, in data, 0);
    }

    //TODO bulk upload
    public void UploadTransform(in DrawTransformPayload payload)
    {
        TransformUtils.CreateNormalMatrix(in payload.Transform, out var normalModel);

        var data = new DrawObjectUniform(
            model: in payload.Transform,
            normal: in normalModel
        );

        _gfxBuffers.UploadUniformGpuData(_drawUbo.Id, in data, _drawUbo.NextUploadCursor());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject()
    {
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, _drawUbo.NextDrawCursor(), _drawUbo.Stride);
    }

    public void DrawMesh(in DrawCommand cmd)
    {
        BindDrawMaterial(cmd.MaterialId);
        BindDrawObject();
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }

}