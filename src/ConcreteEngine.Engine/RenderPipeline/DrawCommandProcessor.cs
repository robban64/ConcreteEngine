using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render;
using ConcreteEngine.Core.Engine.Render.Components;
using ConcreteEngine.Engine.Systems;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.RenderPipeline;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrame()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
    }

    public void PrepareDrawPass()
    {
        _lastAnimationSlot = 0;
        _lastMaterialId = default;
    }

    public void DrawSource(DrawSource source, DrawEntityIndex entity)
    {
        GfxCmd.BindUniformBufferRange<TransformUniform>(entity.SubmitIndex, 1);

        BindMaterial(source.Material);

        if ((source.DrawMask & EntityDrawMask.Skinned) != 0)
        {
            var slot = RenderWorld.Sparse<SkinningLink>().GetUnchecked(entity.Entity).AnimationSlot;
            BindSkinningSlot(slot);
        }

        if ((source.DrawMask & EntityDrawMask.Instanced) != 0)
        {
            var instances = RenderWorld.Sparse<DrawInstancedComponent>().GetUnchecked(entity.Entity).Instances;
            GfxCmd.DrawMeshInstanced(source.Mesh, instances);
            return;
        }

        GfxCmd.DrawMesh(source.Mesh);
    }

    public void BindSkinningSlot(int slot)
    {
        if (slot == _lastAnimationSlot) return;
        _lastAnimationSlot = slot;

        var range = _animationSystem.GetSlotRange(slot - 1);
        GfxCmd.BindUniformBufferRange<SkinningUniform>(range.Offset, range.Length);
    }

    public void BindMaterial(Id16<Material> materialId)
    {
        if (_lastMaterialId == materialId) return;
        _lastMaterialId = materialId;

        var textureBindings = _materialSystem.GetMetaAndSlots(materialId, out var materialMeta);

        var gfxCmd = GfxCmd;
        gfxCmd.BindUniformBufferRange<MaterialUniform>(materialId.Index, 1);

        gfxCmd.ApplyState(materialMeta.DrawState);
        gfxCmd.ApplyStateFunctions(materialMeta.DrawFunctions);

        gfxCmd.UseShader(RenderContext.ResolveShader(materialMeta.ShaderId));
        foreach (var it in textureBindings)
        {
            gfxCmd.BindTextureSlot(it.Texture, (byte)it.Slot);
            gfxCmd.BindSampler(it.Profile, (byte)it.Slot);
        }
    }
}