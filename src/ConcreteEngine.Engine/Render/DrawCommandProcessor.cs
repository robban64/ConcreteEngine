using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
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

    public readonly GfxCommands GfxCmd;
    public readonly GfxBuffers GfxBuffers;
    private readonly AnimationSystem _animationSystem;
    private readonly MaterialSystem _materialSystem;

    internal DrawCommandProcessor(GfxContext gfx, AnimationSystem animationSystem, MaterialSystem materialSystem)
    {
        _animationSystem = animationSystem;
        _materialSystem = materialSystem;
        GfxCmd = gfx.Commands;
        GfxBuffers = gfx.Buffers;
    }

    public void ResetFrame()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
        RenderContext.ResetContext();
    }

    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;

        if (RenderContext.RenderMode == RenderTargetKind.Shadow)
        {
            GfxCmd.UseShader(RenderRegistry.DepthShader);
            GfxCmd.UnbindAllTextures();
        }

        GfxCmd.BindTextureAndSampler(RenderContext.DepthTexture, SamplerProfile.ShadowCompare,
            RenderContext.ShadowSamplerSlot);
    }


    public void DrawFullscreenQuad(ShaderId shaderId, ReadOnlySpan<TextureId> sources)
    {
        GfxCmd.UseShader(shaderId);
        
        for (var i = 0; i < sources.Length; i++) 
            GfxCmd.BindTextureAndSampler(sources[i], SamplerProfile.PointClamp, i);

        GfxCmd.DrawMesh(GfxMeshes.FsqQuad);
    }


    public void DrawSource(RenderSource source, RenderEntityId entity, int submitIndex)
    {
        GfxCmd.BindUniformBufferRange<DrawObjectUniform>(submitIndex, 1);

        BindMaterial(source.Material);

        if ((source.DrawFlags & EntityDrawFlags.Skinned) != 0)
            BindSkinningSlot(entity);

        if ((source.DrawFlags & EntityDrawFlags.Instanced) == 0)
        {
            GfxCmd.DrawMesh(source.Mesh);
        }
        else
        {
            var instances = RenderEcs.Store<DrawInstancedComponent>().Get(entity).Instances;
            GfxCmd.DrawMeshInstanced(source.Mesh, instances);
        }
    }

    public void BindSkinningSlot(RenderEntityId entity)
    {
        var slot = RenderEcs.Store<SkinningLink>().Get(entity).AnimationSlot;
        if (slot != _lastAnimationSlot)
        {
            _lastAnimationSlot = slot;
            var range = _animationSystem.GetSlotRange(slot - 1);
            GfxCmd.BindUniformBufferRange<DrawAnimationUniform>(range.Offset, range.Length);
        }
    }

    public void BindMaterial(Id16<Material> materialId)
    {
        if (_lastMaterialId == materialId) return;
        _lastMaterialId = materialId;

        GfxCmd.BindUniformBufferRange<MaterialUniform>(materialId.Index(), 1);
        var textureBindings = _materialSystem.GetMetaAndSlots(materialId, out var materialMeta);

        GfxCmd.ApplyState(materialMeta.DrawState);
        GfxCmd.ApplyStateFunctions(materialMeta.DrawFunctions);

        GfxCmd.UseShader(RenderContext.ResolveShader(materialMeta.ShaderId));
        foreach (var it in textureBindings)
        {
            GfxCmd.BindTextureSlot(it.Texture, (byte)it.Slot);
            GfxCmd.BindSampler(it.Profile, (byte)it.Slot);
        }
    }

}