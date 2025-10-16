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

    private readonly MaterialStore _materialStore;

    private readonly DrawStateContext _ctx;
    
    private RenderUbo _drawUbo = null!;
    private RenderUbo _materialUbo = null!;

    private MaterialId _prevMaterialId = new (-1);
    
    internal DrawProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload,
        MaterialStore materialStore)
    {
        _ctx = ctx;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxBuffers = ctxPayload.Gfx.Buffers;
        _registry = ctxPayload.Registry;
        _materialStore = materialStore;
    }


    public void Initialize()
    {
        _drawUbo = _registry.GetRenderUbo<DrawObjectUniform>();
        _materialUbo = _registry.GetRenderUbo<MaterialUniformRecord>();
    }

    public void PrepareFrame(nint drawCapacity, nint materialCapacity)
    {
        _prevMaterialId = default;

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

    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);

    public void PrepareDrawPass()
    {
        _drawUbo.ResetCursor();
        _prevMaterialId = default;
        if (_ctx.IsDepth)
            UseShader(_ctx.DepthShader);
    }


    private void BindDrawMaterial(MaterialId materialId)
    {
        if (_prevMaterialId == materialId) return;
        var material = _materialStore.Get(materialId);
        UseShader(_materialStore.ResolveShader(material));

        Span<TextureSlotInfo> slots = stackalloc TextureSlotInfo[RenderLimits.TextureSlots];
        var matSlotLength = _materialStore.DrainMaterialTextureSlots(material, slots);

        var slotSpan = slots.Slice(0, matSlotLength);
        for (var i = 0; i < slotSpan.Length; i++)
        {
            var value = slotSpan[i];
            
            if(value.SlotKind == TextureSlotKind.Shadowmap)
                _gfxCmd.BindTexture(_ctx.DepthTexture, i);
            else
                _gfxCmd.BindTexture(value.Texture, i);

        }

        BindMaterialObject(materialId);

        _prevMaterialId = materialId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindMaterialObject(MaterialId matId)
    {
        var cursor = _materialUbo.SetDrawCursor(matId.Id - 1);
        _gfxBuffers.BindUniformBufferRange(_materialUbo.Id, cursor, _materialUbo.Stride);
    }

    public void UploadMaterialRecord(MaterialId materialId, in MaterialUniformRecord data)
        => _gfxBuffers.UploadUniformGpuData(_materialUbo.Id, in data, 0);

    public void UploadMaterial(ReadOnlySpan<DrawMaterialMeta> commands, ReadOnlySpan<MaterialUniformRecord> data)
        => _gfxBuffers.UploadUniformGpuSpan(_materialUbo.Id, data, _materialUbo.SetUploadCursor(0));

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
        BindDrawMaterial(cmd.MaterialId);
        BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}