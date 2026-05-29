using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxDraw _gfxDraw;
    private readonly UniformUploader _buffers;
    private readonly DrawStateContext _ctx;

    private int _lastAnimationSlot;

    internal DrawCommandProcessor(
        DrawStateContext ctx,
        DrawStateContextPayload ctxPayload,
        UniformUploader buffers)
    {
        _ctx = ctx;
        _buffers = buffers;
        _gfxCmd = ctxPayload.Gfx.Commands;
        _gfxDraw = ctxPayload.Gfx.Draw;
    }


    public void Initialize()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Prepare()
    {
        _lastAnimationSlot = 0;
        _ctx.ResetState();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        _ctx.ResetMaterialState();
        if (_ctx.IsDepth)
        {
            _gfxCmd.UseShader(_ctx.CoreShaders.DepthShader);
            _gfxCmd.UnbindAllTextures();
        }
    }

    public void DrawMesh(DrawCommand cmd, int submitIdx)
    {
        if (_ctx.PrevMaterial != cmd.MaterialId) BindMaterial(cmd.MaterialId);

        if (cmd.AnimationSlot > 0 && cmd.AnimationSlot != _lastAnimationSlot)
        {
            _lastAnimationSlot = cmd.AnimationSlot;
            _buffers.BindAnimation(cmd.AnimationSlot - 1);
        }

        _buffers.BindDrawObject(submitIdx);
        _gfxDraw.BindDraw(cmd.MeshId, cmd.InstanceCount);
    }

    public void DrawSpecialResolveMesh(DrawCommand cmd, DrawCommandResolver resolver, byte resolverSlot, int submitIdx)
    {
        if (!_ctx.IsDepth)
        {
            BindAndResolvedOverride(cmd, resolver, resolverSlot);
        }

        _buffers.BindDrawObject(submitIdx);
        _gfxDraw.BindDraw(cmd.MeshId, cmd.InstanceCount);
    }

    private void BindMaterial(MaterialId materialId)
    {
        var texSlots = _buffers.ResolveMaterial(materialId, out var materialMeta);

        if (!materialMeta.PassState.IsEmpty) BindPassState(in materialMeta);

        if (_ctx.PassMode == PassStateMode.Main)
        {
            _gfxCmd.UseShader(materialMeta.ShaderId);
            if (texSlots.Length > 0) BindTextureSlots(texSlots);
        }
        else if (_ctx.PassMode == PassStateMode.Depth && texSlots.Length > 0)
        {
            BindDepthTextureSlots(texSlots);
        }
    }

    private void BindTextureSlots(ReadOnlySpan<TextureBinding> slots)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var value = slots[i];
            var texture = value.SlotKind != TextureUsage.Shadowmap ? value.Texture : _ctx.DepthTexture;
            _gfxCmd.BindTexture(texture, i);
        }
    }

    private void BindDepthTextureSlots(ReadOnlySpan<TextureBinding> slots)
    {
        _gfxCmd.BindTexture(slots[0].Texture, 0);

        for (var i = 1; i < slots.Length; i++)
        {
            var value = slots[i];
            if (value.SlotKind != TextureUsage.Mask) continue;
            _gfxCmd.BindTexture(value.Texture, 1);
            return;
        }

        _gfxCmd.BindTexture(GfxTextures.Fallback.AlphaMaskId, 1);
    }

    private void BindPassState(in RenderMaterialMeta material)
    {
        _gfxCmd.ApplyState(!material.PassState.IsEmpty ? material.PassState : _ctx.PassState);
        _gfxCmd.ApplyStateFunctions(material.PassFunctions != default ? material.PassFunctions : _ctx.PassFunctions);
    }

    // allow for more flexible state management later on
    private void BindAndResolvedOverride(DrawCommand cmd, DrawCommandResolver resolver, byte resolverSlot)
    {
        const GfxStateFlags allowMaterialOverride = GfxStateFlags.Cull | GfxStateFlags.PolygonOffset |
                                                    GfxStateFlags.Blend | GfxStateFlags.DepthWrite;

        Debug.Assert(resolver is DrawCommandResolver.Highlight or DrawCommandResolver.BoundingVolume);

        var isAnimated = cmd.AnimationSlot > 0;

        ShaderId shader;
        switch (resolver)
        {
            case DrawCommandResolver.Highlight:
                shader = _ctx.CoreShaders.HighlightShader;
                break;
            case DrawCommandResolver.BoundingVolume:
                isAnimated = false;
                shader = _ctx.CoreShaders.BoundingBoxShader;
                break;
            case DrawCommandResolver.Wireframe:
            default:
                Throwers.Unreachable(nameof(resolver));
                return;
        }

        if (isAnimated) _buffers.BindAnimation(cmd.AnimationSlot - 1);

        _gfxCmd.UseShader(shader);

        _buffers.UploadEditorEffectUniform(resolverSlot, isAnimated);

        var texSlots = _buffers.ResolveMaterial(cmd.MaterialId, out var materialMeta);
        foreach (var slot in texSlots)
        {
            if (slot.SlotKind == TextureUsage.Albedo) _gfxCmd.BindTexture(slot.Texture, 0);
            else if (slot.SlotKind == TextureUsage.Mask) _gfxCmd.BindTexture(slot.Texture, 1);
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

        if (materialMeta.PassFunctions == default)
        {
            _ctx.OverridePassFunctions = default;
        }
        else if (_ctx.OverridePassFunctions != materialMeta.PassFunctions)
        {
            var f = _ctx.PassFunctions;
            var m = materialMeta.PassFunctions;
            _ctx.OverridePassFunctions = f with
            {
                PolygonOffset = m.PolygonOffset == PolygonOffsetLevel.Unset ? f.PolygonOffset : m.PolygonOffset,
                Cull = m.Cull == CullMode.Unset ? f.Cull : m.Cull
            };
        }
    }
}