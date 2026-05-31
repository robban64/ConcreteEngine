using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;
using ConcreteEngine.Renderer.Passes;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer;

internal sealed class DrawCommandProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxDraw _gfxDraw;
    private readonly UniformUploader _buffers;

    public TextureId DepthTexture { get; private set; }

    private int _lastAnimationSlot;

    private static PassStateMode PassMode => RenderContext.Instance.PassMode;

    internal DrawCommandProcessor(GfxContext gfx, RenderRegistry renderRegistry, UniformUploader buffers)
    {
        _buffers = buffers;
        _gfxCmd = gfx.Commands;
        _gfxDraw = gfx.Draw;

        var depthFbo = renderRegistry.FboRegistry.GetByKey(PassTags<ShadowPassTag>.FboKey(FboVariant.V0));
        DepthTexture = depthFbo!.Attachments.DepthTexture;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Prepare()
    {
        _lastAnimationSlot = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        if (PassMode != PassStateMode.Depth) return;

        _gfxCmd.UseShader(RenderShaderRegistry.CoreShaders.DepthShader);
        _gfxCmd.UnbindAllTextures();
    }

    public void DrawMesh(DrawCommand cmd, int submitIdx)
    {
        if (_buffers.PrevMaterial != cmd.MaterialId) BindMaterial(cmd.MaterialId);

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
        if (PassMode != PassStateMode.Depth)
        {
            BindAndResolvedOverride(cmd, resolver, resolverSlot);
        }

        _buffers.BindDrawObject(submitIdx);
        _gfxDraw.BindDraw(cmd.MeshId, cmd.InstanceCount);
    }

    private void BindMaterial(MaterialId materialId)
    {
        var texSlots = _buffers.ResolveMaterial(materialId, out var materialMeta);

        if (!materialMeta.DrawState.IsEmpty())
        {
            _gfxCmd.ApplyState(materialMeta.DrawState);
            _gfxCmd.ApplyStateFunctions(materialMeta.PassFunctions);
        }

        if (PassMode == PassStateMode.Depth && texSlots.Length > 0)
        {
            BindDepthTextureSlots(texSlots);
            return;
        }

        _gfxCmd.UseShader(materialMeta.ShaderId);
        if (texSlots.Length > 0)
            BindTextureSlots(texSlots, materialMeta.ShadowMapBinding);
    }

    private void BindTextureSlots(ReadOnlySpan<TextureBinding> slots, sbyte shadowMapBinding)
    {
        if (shadowMapBinding >= 0)
            _gfxCmd.BindTexture(DepthTexture, shadowMapBinding);

        foreach (var value in slots)
        {
            if (value.Slot < 0) continue;
            _gfxCmd.BindTexture(value.Texture, value.Slot);
        }
    }

    private void BindDepthTextureSlots(ReadOnlySpan<TextureBinding> slots)
    {
        //_gfxCmd.BindTexture(GfxTextures.Fallback.AlphaMaskId, 1);

        foreach (var value in slots)
        {
            if (value.SlotKind == TextureUsage.Albedo)
                _gfxCmd.BindTexture(value.Texture, 0);
            else if (value.SlotKind == TextureUsage.Mask)
                _gfxCmd.BindTexture(value.Texture, 1);
        }
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
                shader = RenderShaderRegistry.CoreShaders.HighlightShader;
                break;
            case DrawCommandResolver.BoundingVolume:
                isAnimated = false;
                shader = RenderShaderRegistry.CoreShaders.BoundingBoxShader;
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
    }
}