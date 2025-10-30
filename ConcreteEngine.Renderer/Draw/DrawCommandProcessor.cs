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

    private void BindTextureSlots(ReadOnlySpan<TextureSlotInfo> slots)
    {
        if (_ctx.IsDepth)
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

            return;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            var value = slots[i];
            if (value.SlotKind == TextureSlotKind.Shadowmap)
                _gfxCmd.BindTexture(_ctx.DepthTexture, i);
            else
                _gfxCmd.BindTexture(value.Texture, i);
        }
    }

    private void BindMaterial(MaterialId materialId)
    {
        var texSlots = _buffers.ResolveMaterial(materialId, out var materialMeta);
        //if (!_ctx.IsDepth)
        // use regular for now.
        UseShader(materialMeta.ShaderId);

        BindTextureSlots(texSlots);
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