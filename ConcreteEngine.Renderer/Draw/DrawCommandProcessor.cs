using System.Diagnostics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Renderer.Draw;

internal sealed class DrawCommandProcessor
{
    private readonly Color4 _highlightColor = Color4.FromRgba(46, 163, 242);

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


    public void DrawMesh(DrawCommand cmd, DrawCommandTicket ticket)
    {
        if (_ctx.PrevMaterial != cmd.MaterialId)
        {
            var texSlots = _buffers.ResolveMaterial(cmd.MaterialId, out var materialMeta);

            if (!materialMeta.PassState.IsEmpty) BindPassState(in materialMeta);

            switch (_ctx.PassMode)
            {
                case PassStateMode.Main:
                    _gfxCmd.UseShader(materialMeta.ShaderId);
                    if (texSlots.Length > 0) BindTextureSlots(texSlots);
                    break;
                case PassStateMode.Depth:
                    if (texSlots.Length > 0) BindDepthTextureSlots(texSlots);
                    break;
            }
        }

        if (ticket.AnimationSlot > 0) _buffers.BindAnimation(ticket.AnimationSlot - 1);

        _buffers.BindDrawObject(ticket.SubmitIdx);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawMesh(cmd.MeshId, cmd.DrawCount);
    }


    public void DrawSpecialResolveMesh(DrawCommand cmd, DrawCommandTicket ticket)
    {
        if (!_ctx.IsDepth)
        {
            BindAndResolvedOverride(cmd, ticket);
        }

        _buffers.BindDrawObject(ticket.SubmitIdx);
        _gfxCmd.BindMesh(cmd.MeshId);
        _gfxCmd.DrawMesh(cmd.MeshId, cmd.DrawCount);
    }

    private void BindTextureSlots(ReadOnlySpan<TextureSlotInfo> slots)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var value = slots[i];
            _gfxCmd.BindTexture(value.SlotKind != TextureSlotKind.Shadowmap ? value.Texture : _ctx.DepthTexture, i);
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

    private void BindPassState(in DrawMaterialMeta materialMeta)
    {
        if (!materialMeta.PassState.IsEmpty)
        {
            _gfxCmd.ApplyState(_ctx.OverridePassState = materialMeta.PassState);
        }
        else if (!_ctx.OverridePassState.IsEmpty)
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

    // allow for more flexible state management later on
    private void BindAndResolvedOverride(DrawCommand cmd, DrawCommandTicket ticket)
    {
        const GfxStateFlags allowMaterialOverride = GfxStateFlags.Cull | GfxStateFlags.PolygonOffset;

        Debug.Assert(ticket.Resolver is DrawCommandResolver.Highlight or DrawCommandResolver.BoundingVolume);

        var texSlots = _buffers.ResolveMaterial(cmd.MaterialId, out var materialMeta);
        ref readonly var shaders = ref _ctx.CoreShaders;

        switch (ticket.Resolver)
        {
            case DrawCommandResolver.Highlight:
                if (ticket.AnimationSlot > 0)
                {
                    _buffers.BindAnimation(ticket.AnimationSlot - 1);
                    _gfxCmd.UseShader(shaders.HighlightShader, _ctx.GetUniformLocations(shaders.HighlightShader));
                    _gfxCmd.SetUniform(0, 1);
                    _gfxCmd.SetUniform(1, in _highlightColor);
                    break;
                }

                _gfxCmd.UseShader(shaders.HighlightShader, _ctx.GetUniformLocations(shaders.HighlightShader));
                _gfxCmd.SetUniform(0, 0);
                _gfxCmd.SetUniform(1, in _highlightColor);
                break;
            case DrawCommandResolver.BoundingVolume:
                _gfxCmd.UseShader(shaders.BoundingBoxShader, _ctx.GetUniformLocations(shaders.BoundingBoxShader));
                _gfxCmd.SetUniform(0, Color4.Green);
                break;
            case DrawCommandResolver.Wireframe:
            default:
                throw new NotSupportedException();
        }

        foreach (var slot in texSlots)
        {
            if (slot.SlotKind == TextureSlotKind.Albedo) _gfxCmd.BindTexture(slot.Texture, 0);
            else if (slot.SlotKind == TextureSlotKind.Mask) _gfxCmd.BindTexture(slot.Texture, 1);
        }

        if (materialMeta.PassState.IsEmpty)
        {
            _ctx.OverridePassState = default;
        }
        else
        {
            var filtered = materialMeta.PassState.Filter(allowMaterialOverride);
            _ctx.OverridePassState = GfxPassState.PatchWith(_ctx.PassState, filtered);
        }

        if (materialMeta.PassStateFunc == default)
        {
            _ctx.OverridePassStateFunc = default;
        }
        else if (_ctx.OverridePassStateFunc != materialMeta.PassStateFunc)
        {
            var f = _ctx.PassStateFunc;
            var m = materialMeta.PassStateFunc;
            _ctx.OverridePassStateFunc = f with
            {
                PolygonOffset = m.PolygonOffset == PolygonOffsetLevel.Unset ? f.PolygonOffset : m.PolygonOffset,
                Cull = m.Cull == CullMode.Unset ? f.Cull : m.Cull
            };
        }
    }
}