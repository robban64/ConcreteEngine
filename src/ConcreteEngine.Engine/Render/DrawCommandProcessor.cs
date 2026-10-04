using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.Components;
using ConcreteEngine.Core.Engine.Graphics;
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

    public void DrawSource(DrawEntityContext ctx, int submitIndex)
    {
        GfxCmd.BindUniformBufferRange<TransformUniform>(submitIndex, 1);

        BindMaterial(ctx.Source.Material);

        var drawFlags = ctx.Source.DrawFlags;
        if ((drawFlags & EntityDrawFlags.Skinned) != 0)
        {
            var slot = ctx.GetComponent<SkinningLink>().AnimationSlot;
            BindSkinningSlot(slot);
        }

        if ((drawFlags & EntityDrawFlags.Instanced) != 0)
        {
            var instances = ctx.GetComponent<DrawInstancedComponent>().Instances;
            GfxCmd.DrawMeshInstanced(ctx.Source.Mesh, instances);
            return;
        }

        GfxCmd.DrawMesh(ctx.Source.Mesh);
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