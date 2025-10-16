#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Commands;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Core.Rendering.State;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Draw;

internal sealed class DrawProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly RenderRegistry _registry;

    private readonly RenderMaterialRegistry _materialRegistry;

    private readonly DrawStateContext _ctx;

    private MaterialId _previousMaterialId = new (-1);

    private RenderUbo _drawUbo = null!;
    private RenderUbo _materialUbo = null!;

    internal DrawProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload,
        RenderMaterialRegistry materialRegistry)
    {
        _ctx = ctx;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _registry = ctxPayload.Registry;
        _materialRegistry = materialRegistry;
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
        var material = _materialRegistry.GetMaterial(materialId);
        UseShader(material.ShaderId);

        for (var i = 0; i < material.SamplerSlots.Length; i++)
        {
            var value = material.SamplerSlots[i];
            
            if(value.SlotKind == TextureSlotKind.Shadowmap)
                _gfxCmd.BindTexture(_ctx.DepthTexture, i);
            else
                _gfxCmd.BindTexture(value.Texture, i);

        }

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