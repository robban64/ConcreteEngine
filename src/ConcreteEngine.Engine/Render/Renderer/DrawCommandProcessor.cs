using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Buffers;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Engine.Render.Registry;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render.Renderer;

internal sealed class DrawCommandProcessor
{
    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly MaterialBuffer _materialBuffer;
    private readonly SkinningBuffer _skinningBuffer;
    private readonly EffectBuffer _effectBuffer;

    private int _lastAnimationSlot;
    private Id16<Material> _lastMaterialId;

    private static PassStateMode PassMode => RenderContext.Instance.PassMode;

    internal DrawCommandProcessor(GfxContext gfx, RenderUploadBuffers buffers)
    {
        _gfxCmd = gfx.Commands;
        _gfxBuffers = gfx.Buffers;
        _materialBuffer = buffers.Materials;
        _skinningBuffer = buffers.Skinning;
        _effectBuffer = buffers.Effects;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Prepare()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
        if (PassMode != PassStateMode.Depth) return;

        _gfxCmd.UseShader(RenderRegistry.DepthShader);
        _gfxCmd.UnbindAllTextures();
    }

    public void DrawMesh(DrawCommand cmd, int submitIdx)
    {
         BindMaterial(cmd.MaterialId);

        if (cmd.AnimationSlot > 0 && cmd.AnimationSlot != _lastAnimationSlot)
        {
            _lastAnimationSlot = cmd.AnimationSlot;
            BindAnimation(cmd.AnimationSlot - 1);
        }

        BindDrawObject(submitIdx);
        _gfxCmd.DrawMesh(cmd.MeshId, cmd.InstanceCount);
    }

    public void DrawSpecialResolveMesh(DrawCommand cmd, int submitIdx)
    {
        if (PassMode != PassStateMode.Depth)
        {
            BindAndResolvedOverride(cmd, cmd.Resolver, cmd.ResolverSlot);
        }

        BindDrawObject(submitIdx);
        _gfxCmd.DrawMesh(cmd.MeshId, cmd.InstanceCount);
    }

    private void BindMaterial(Id16<Material> materialId)
    {
        if (_lastMaterialId == materialId) return;
        
        var textureBindings = BindResolveMaterial(materialId, out var materialMeta);

        if (!materialMeta.DrawState.IsEmpty())
        {
            _gfxCmd.ApplyState(materialMeta.DrawState);
            _gfxCmd.ApplyStateFunctions(materialMeta.DrawFunctions);
        }

        if (PassMode == PassStateMode.Depth && textureBindings.Length > 0)
        {
            BindDepthTextureSlots(textureBindings);
            return;
        }
        _gfxCmd.UseShader(materialMeta.ShaderId);
        BindTextureSlots(textureBindings, materialMeta.ShadowMapBinding);
    }

    private void BindTextureSlots(ReadOnlySpan<TextureBinding> slots, sbyte shadowMapBinding)
    {
        if (shadowMapBinding >= 0)
            _gfxCmd.BindTexture(RenderContext.Instance.DepthTexture, shadowMapBinding);

        foreach (var value in slots)
        {
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
        ShaderId shader;
        var isAnimated = cmd.AnimationSlot > 0;
        switch (resolver)
        {
            case DrawCommandResolver.Highlight: shader = RenderRegistry.HighlightShader; break;
            case DrawCommandResolver.BoundingVolume:
                isAnimated = false;
                shader = RenderRegistry.BoundingBoxShader;
                break;
            case DrawCommandResolver.Wireframe:
            default: Throwers.Unreachable(nameof(resolver)); return;
        }

        if (isAnimated) BindAnimation(cmd.AnimationSlot - 1);
        _gfxCmd.UseShader(shader);
        UploadEditorEffectUniform(resolverSlot, isAnimated);
        var texSlots = BindResolveMaterial(cmd.MaterialId, out var materialMeta);
        foreach (var slot in texSlots)
        {
            if (slot.SlotKind == TextureUsage.Albedo) _gfxCmd.BindTexture(slot.Texture, 0);
            else if (slot.SlotKind == TextureUsage.Mask) _gfxCmd.BindTexture(slot.Texture, 1);
        }
    }
    
    private ReadOnlySpan<TextureBinding> BindResolveMaterial(Id16<Material> materialId,
        out MaterialMeta materialMeta)
    {
        if (_lastMaterialId != materialId)
        {
            _lastMaterialId = materialId;
            _gfxBuffers.BindUniformBufferRange<MaterialUniform>(materialId.Index(), 1);
            return _materialBuffer.GetMetaAndSlots(materialId, out materialMeta);
        }

        materialMeta = default;
        return ReadOnlySpan<TextureBinding>.Empty;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindDrawObject(int submitIndex)
    {
        _gfxBuffers.BindUniformBufferRange<DrawObjectUniform>(submitIndex , 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BindAnimation(int slot)
    {
        var range = _skinningBuffer.GetSlotRange(slot);
        _gfxBuffers.BindUniformBufferRange<DrawAnimationUniform>(range.Offset , range.Length);
    }

    private unsafe void UploadEditorEffectUniform(byte slot, bool isAnimated)
    {
        ref readonly var effect = ref _effectBuffer.Get(slot);
        var data = new EditorEffectsUniform(isAnimated, effect.Color);
        _gfxBuffers.UploadSingleUniform(&data, 0);
    }

}