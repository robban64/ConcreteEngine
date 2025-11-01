#region

using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawCommandProcessor
{
    private readonly GfxCommands _gfxCmd;

    private readonly DrawBuffers _buffers;
    private readonly DrawStateContext _ctx;

    internal DrawCommandProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload,
        DrawBuffers buffers)
    {
        _ctx = ctx;
        _buffers = buffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
    }


    public void Initialize()
    {
    }

    public void Prepare() => _ctx.ResetState();

    public void PrepareDrawPass()
    {
        _ctx.ResetMaterialState();
        if (_ctx.IsDepth)
        {
            UseShader(_ctx.CoreShaders.DepthShader);
            _gfxCmd.UnbindAllTextures();
        }
    }

    private void UseShader(ShaderId shaderId) => _gfxCmd.UseShader(shaderId);

    private void BindDepthTextureSlots(ReadOnlySpan<TextureSlotInfo> slots)
    {
        _gfxCmd.BindTexture(slots[0].Texture, 0);

        for (var i = 1; i < slots.Length; i++)
        {
            var value = slots[i];
            if (value.SlotKind != TextureSlotKind.Mask) continue;
            _gfxCmd.BindTexture(value.Texture, 1);
            return;
        }

        _gfxCmd.BindTexture(GfxTextures.FallbackTextures.AlphaMaskId, 1);

    }

    private void BindTextureSlots(ReadOnlySpan<TextureSlotInfo> slots)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var value = slots[i];
            _gfxCmd.BindTexture(value.SlotKind != TextureSlotKind.Shadowmap ? value.Texture : _ctx.DepthTexture, i);
        }
    }

    private void BindMaterial(MaterialId materialId)
    {
        var texSlots = _buffers.ResolveMaterial(materialId, out var materialMeta);

        switch (_ctx.PassMode)
        {
            case PassStateMode.Main:
                UseShader(materialMeta.ShaderId);
                BindTextureSlots(texSlots);
                break;
            case PassStateMode.Depth:
                BindDepthTextureSlots(texSlots);
                break;
        }
            
        _buffers.BindMaterialObject(materialId);

        if (materialMeta.PassState != default)
        {
            _gfxCmd.ApplyState(_ctx.OverridePassState = materialMeta.PassState);
        }
        else if (_ctx.OverridePassState != default)
        {
            _ctx.OverridePassState = default;
            _gfxCmd.ApplyState(_ctx.PassState);
        }

        if (materialMeta.PassStateFunc != default)
        {
            _gfxCmd.ApplyStateFunctions(_ctx.OverridePassStateFunc = materialMeta.PassStateFunc);
        }
        else if (_ctx.OverridePassStateFunc != default)
        {
            _ctx.OverridePassStateFunc = default;
            _gfxCmd.ApplyStateFunctions(_ctx.PassStateFunc);
        }
    }

    public void DrawMesh(DrawCommand cmd, int submitIndex)
    {
        if (_ctx.PrevMaterial != cmd.MaterialId) BindMaterial(cmd.MaterialId);

        _buffers.BindDrawObject(submitIndex);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}