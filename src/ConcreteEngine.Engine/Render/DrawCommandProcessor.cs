using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;
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

    public void DrawSource(RenderEntityCore.RenderEntityContext ctx, int submitIndex)
    {
        GfxCmd.BindUniformBufferRange<TransformUniform>(submitIndex, 1);

        var source = ctx.Source;
        
        BindMaterial(source.Material);

        if ((source.DrawFlags & EntityDrawFlags.Skinned) != 0)
        {
            var slot = ctx.GetComponent<SkinningLink>().AnimationSlot;
            BindSkinningSlot(slot);
        }

        if ((source.DrawFlags & EntityDrawFlags.Instanced) == 0)
        {
            GfxCmd.DrawMesh(source.Mesh);
        }
        else
        {
            var instances = ctx.GetComponent<DrawInstancedComponent>().Instances;
            GfxCmd.DrawMeshInstanced(source.Mesh, instances);
        }
    }
    public void BindSkinningSlot(int slot)
    {
        if (slot != _lastAnimationSlot)
        {
            _lastAnimationSlot = slot;
            var range = _animationSystem.GetSlotRange(slot - 1);
            GfxCmd.BindUniformBufferRange<SkinningUniform>(range.Offset, range.Length);
        }
    }


    public void BindMaterial(Id16<Material> materialId)
    {
        if (_lastMaterialId == materialId) return;
        _lastMaterialId = materialId;

        GfxCmd.BindUniformBufferRange<MaterialUniform>(materialId.Index, 1);
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