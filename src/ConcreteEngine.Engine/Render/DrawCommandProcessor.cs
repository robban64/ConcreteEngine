using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal sealed class DrawCommandProcessor
{
    private int _lastAnimationSlot;
    private Id16<Material> _lastMaterialId;

    private readonly GfxCommands _gfxCmd;
    private readonly GfxBuffers _gfxBuffers;
    private readonly AnimationSystem _animationSystem;
    private readonly MaterialSystem _materialSystem;

    internal DrawCommandProcessor(GfxContext gfx, AnimationSystem animationSystem, MaterialSystem materialSystem)
    {
        _animationSystem = animationSystem;
        _materialSystem = materialSystem;
        _gfxCmd = gfx.Commands;
        _gfxBuffers = gfx.Buffers;
    }

    public void ResetFrame()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
    }

    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;

        if (RenderContext.RenderMode == RenderTargetKind.Shadow)
        {
            _gfxCmd.UseShader(RenderRegistry.DepthShader);
            _gfxCmd.UnbindAllTextures();
        }
        
        _gfxCmd.BindTexture(RenderContext.DepthTexture, SamplerProfile.ShadowCompare, RenderContext.ShadowSamplerSlot);
    }

    public void DrawSource(RenderSource source, RenderEntityId entity, int submitIndex)
    {
        _gfxBuffers.BindUniformBufferRange<DrawObjectUniform>(submitIndex, 1);

        BindMaterial(source.Material);
        
        if ((source.DrawFlags & EntityDrawFlags.Skinned) != 0)
            BindSkinningSlot(entity);
        
        if((source.DrawFlags & EntityDrawFlags.Instanced) == 0)
        {
            _gfxCmd.DrawMesh(source.Mesh);
        }
        else
        {
            var instances = RenderEcs.Store<DrawInstancedComponent>().Get(entity).Instances;
            _gfxCmd.DrawMeshInstanced(source.Mesh, instances);
        }
    }

    public void BindSkinningSlot(RenderEntityId entity)
    {
        var slot = RenderEcs.Store<SkinningLink>().Get(entity).AnimationSlot;
        if (slot != _lastAnimationSlot)
        {
            _lastAnimationSlot = slot;
            var range = _animationSystem.GetSlotRange(slot - 1);
            _gfxBuffers.BindUniformBufferRange<DrawAnimationUniform>(range.Offset, range.Length);
        }
    }

    private void BindMaterial(Id16<Material> materialId)
    {
        if (_lastMaterialId == materialId) return;
        _lastMaterialId = materialId;

        _gfxBuffers.BindUniformBufferRange<MaterialUniform>(materialId.Index(), 1);
        var textureBindings = _materialSystem.GetMetaAndSlots(materialId, out var materialMeta);

        _gfxCmd.ApplyState(materialMeta.DrawState);
        _gfxCmd.ApplyStateFunctions(materialMeta.DrawFunctions);
        
        if (RenderContext.RenderMode == RenderTargetKind.Scene)
        {
            _gfxCmd.UseShader(materialMeta.ShaderId);
            _gfxCmd.BindTextures(textureBindings.AsReadOnlySpan());
        }
        else
        {
            BindAlbedoMaskSlots(textureBindings);
        }

    }
    
    public bool TryApplyMaterialState(Id16<Material> materialId, out NativeView<TextureBinding> bindings)
    {
        if (_lastMaterialId == materialId)
        {
            bindings = default;
            return false;
        }
        _lastMaterialId = materialId;

        _gfxBuffers.BindUniformBufferRange<MaterialUniform>(materialId.Index(), 1);
        bindings = _materialSystem.GetMetaAndSlots(materialId, out var materialMeta);
        
        _gfxCmd.ApplyState(materialMeta.DrawState);
        _gfxCmd.ApplyStateFunctions(materialMeta.DrawFunctions);

        return true;
    }


    public void BindAlbedoMaskSlots(NativeView<TextureBinding> slots)
    {
        foreach (var value in slots)
        {
            if (value.SlotKind is TextureUsage.Albedo or TextureUsage.Mask)
                _gfxCmd.BindTexture(value.Texture, value.Slot);
        }
    }
    
    
/*
    public void DrawSpecialResolveMesh(RenderSource cmd, RenderEntityId entity, int submitIdx)
    {
        if (RenderContext.PassMode != PassStateMode.Depth)
        {
            BindAndResolvedOverride(cmd, entity, cmd.Resolver, cmd.ResolverSlot);
        }

        BindDrawObject(submitIdx);
        _gfxCmd.DrawMesh(cmd.Mesh, cmd.InstanceCount);
    }
*/

/*
    // allow for more flexible state management later on
    private void BindAndResolvedOverride(RenderSource cmd, RenderEntityId entity, DrawCommandResolver resolver,
        byte resolverSlot)
    {
        ShaderId shader;
        var isAnimated = cmd.Kind == EntitySourceKind.AnimatedModel;
        switch (resolver)
        {
            case DrawCommandResolver.Highlight:
                shader = RenderRegistry.HighlightShader;
                break;
            case DrawCommandResolver.BoundingVolume:
                isAnimated = false;
                shader = RenderRegistry.BoundingBoxShader;
                break;
            case DrawCommandResolver.Wireframe:
            default:
                Throwers.Unreachable(nameof(resolver));
                return;
        }

        if (isAnimated)
        {
            var slot = RenderEcs.GetRenderStore<SkinningComponent>().Get(entity).AnimationSlot;
            if (slot != _lastAnimationSlot)
            {
                _lastAnimationSlot = slot;
                BindAnimation(slot - 1);
            }
        }

        _gfxCmd.UseShader(shader);
        UploadEditorEffectUniform(resolverSlot, isAnimated);
        var texSlots = BindResolveMaterial(cmd.Material, out var materialMeta);
        foreach (var slot in texSlots)
        {
            if (slot.SlotKind == TextureUsage.Albedo) _gfxCmd.BindTexture(slot.Texture, 0);
            else if (slot.SlotKind == TextureUsage.Mask) _gfxCmd.BindTexture(slot.Texture, 1);
        }
    }
*/
 
}