#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
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
            _gfxCmd.UseShader(_ctx.CoreShaders.DepthShader);
            _gfxCmd.UnbindAllTextures();
        }
    }


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

    private void BindPassState(in DrawMaterialMeta materialMeta)
    {
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

    private void BindMaterial(MaterialId materialId)
    {
        var texSlots = _buffers.ResolveMaterial(materialId, out var materialMeta);

        switch (_ctx.PassMode)
        {
            case PassStateMode.Main:
                _gfxCmd.UseShader(materialMeta.ShaderId);
                BindTextureSlots(texSlots);
                break;
            case PassStateMode.Depth:
                BindDepthTextureSlots(texSlots);
                break;
        }

        _buffers.BindMaterialObject(materialId);
        BindPassState(in materialMeta);
    }

    private void ApplyResolvedOverride(DrawCommand cmd, DrawCommandTicket ticket)
    {
        var texSlots = _buffers.ResolveMaterialNoCache(cmd.MaterialId, out var materialMeta);
        
        switch (ticket.Resolver)
        {
            case DrawCommandResolver.Wireframe:
                InvalidOpThrower.ThrowIf(true, "Not supported resolver");
                break;
            case DrawCommandResolver.Highlight:
                var shader = _ctx.CoreShaders.HighlightShader;
                _gfxCmd.UseShader(shader, _ctx.GetUniformLocations(shader));
                _gfxCmd.SetUniform(0, Color4.FromRgba(46, 163, 242));
                foreach (var slot in texSlots)
                {
                    if(slot.SlotKind == TextureSlotKind.Albedo) _gfxCmd.BindTexture(slot.Texture, 0);
                    if(slot.SlotKind == TextureSlotKind.Mask) _gfxCmd.BindTexture(slot.Texture, 1);
                }
                break;
        }

        _buffers.BindMaterialObject(cmd.MaterialId);
        BindPassState(in materialMeta);
        
        _gfxCmd.ApplyState(GfxPassState.Enable(GfxStateFlags.Blend));
        _gfxCmd.ApplyStateFunctions(GfxPassStateFunc.MakeDefault() with{ Blend = BlendMode.Alpha, Cull = CullMode.BackCcw});
        _gfxCmd.ApplyState(GfxPassState.Disable(GfxStateFlags.DepthWrite | GfxStateFlags.DepthTest ));

        _ctx.ResetCachedMaterial();
        
    }

    public void DrawMesh(DrawCommand cmd, DrawCommandTicket ticket)
    {
        if (!_ctx.IsDepth && ticket.Resolver != DrawCommandResolver.None)
            ApplyResolvedOverride(cmd, ticket);
        else if ( _ctx.PrevMaterial != cmd.MaterialId)
            BindMaterial(cmd.MaterialId);
        
        _buffers.BindDrawObject(ticket.SubmitIdx);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawBoundMesh(cmd.MeshId, cmd.DrawCount);
    }
}