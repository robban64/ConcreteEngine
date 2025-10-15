#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using MaterialStore = ConcreteEngine.Core.Assets.Materials.MaterialStore;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly RenderRegistry _registry;

    private readonly MaterialStore _materials;

    private readonly DrawStateContext _ctx;

    private MaterialId _previousMaterialId;

    private RenderUbo _drawUbo = null!;
    private RenderUbo _materialUbo = null!;

    internal DrawProcessor(DrawStateContext ctx, DrawStateContextPayload ctxPayload, MaterialStore materials)
    {
        _ctx = ctx;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _registry = ctxPayload.Registry;
        _materials = materials;
    }


    public void Initialize()
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = _registry.GetRenderUbo<MaterialUniformRecord>();
    }

    public void PrepareFrame(nint drawCapacity, nint materialCapacity)
    {
        _previousMaterialId = default;

        _drawUbo.ResetCursor();
        _materialUbo.ResetCursor();

        if (drawCapacity > _drawUbo.Capacity)
        {
            _drawUbo.SetCapacity(drawCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_drawUbo.Id, drawCapacity);
        }

        if (materialCapacity > _materialUbo.Capacity)
        {
            _materialUbo.SetCapacity(materialCapacity);
            _gfxBuffers.SetUniformBufferCapacity(_materialUbo.Id, drawCapacity);
        }
    }

    public void PrepareDrawPass()
    {
        _drawUbo.ResetCursor();
        _previousMaterialId = default;
        if (_ctx.OverrideDrawShader > 0)
            UseShader(_ctx.OverrideDrawShader);
    }

    private void UseShader(ShaderId shaderId)
    {
        //var renderShader = _registry.GetRenderShader(shaderId);
        _gfxCmd.UseShader(shaderId);
    }

    private void BindDrawMaterial(MaterialId materialId)
    {
        if (_previousMaterialId == materialId) return;
        var material = _materials.GetMaterial(materialId);
        UseShader(material.ShaderId);

        for (var i = 0; i < material.SamplerSlots.Length; i++)
        {
            var value = material.SamplerSlots[i];
            _gfxCmd.BindTexture(value, i);
        }

        if (material.Shadows)
            _gfxCmd.BindTexture(_ctx.DepthTexture, material.SamplerSlots.Length);

        //BindMaterialObject(materialId);
        BindMaterialObject(materialId);

        _previousMaterialId = materialId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindMaterialObject(MaterialId matId)
    {
        var cursor = _materialUbo.SetDrawCursor(matId.Id - 1);
        _gfxBuffers.BindUniformBufferRange(_materialUbo.Id, cursor, _materialUbo.Stride);
    }

    public void UploadMaterialRecord(MaterialId materialId, in MaterialUniformRecord data)
        => _gfxBuffers.UploadUniformGpuData(_materialUbo.Id, in data, 0);

    public void UploadMaterial(ReadOnlySpan<DrawMaterialCommand> commands, ReadOnlySpan<MaterialUniformRecord> data)
        => _gfxBuffers.UploadUniformGpuSpan(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));
/*
    public void UploadMaterial(Material mat)
    {
        var data = new MaterialUniformRecord(
            matColor: new Vector4(mat.Color.AsVec3(), 1),
            matParams0: new Vector4(mat.SpecularStrength, mat.UvRepeat, 0.0f, 0.0f),
            matParams1: new Vector4(mat.Shininess, mat.HasNormalMap ? 1.0f : 0.0f, 0.0f, 0.0f)
        );

        _gfxBuffers.UploadUniformGpuData(_materialUbo.Id, in data, 0);
    }
*/


    public void UploadDrawObjects(ReadOnlySpan<DrawObjectUniform> payload)
    {
        _gfxBuffers.UploadUniformGpuSpan(_drawUbo.Id, payload, _drawUbo.SetUploadCursor(0));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject(int submitIndex)
    {
        var cursor = _drawUbo.SetDrawCursor(submitIndex);
        _gfxBuffers.BindUniformBufferRange(_drawUbo.Id, cursor, _drawUbo.Stride);
    }

    public void DrawMesh(DrawCommand cmd, int submitIndex)
    {
        if (_ctx.OverrideDrawShader == default)
            BindDrawMaterial(cmd.MaterialId);

        BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}