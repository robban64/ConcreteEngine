using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render;

internal sealed class FrameProcessor(MaterialStore materialStore)
{
    private bool _hasUploadedMaterial;

    internal void SubmitMaterialData(RenderProgram renderer)
    {
        if (!materialStore.HasDirtyMaterials && _hasUploadedMaterial) return;
        if (materialStore.HasDirtyMaterials) _hasUploadedMaterial = false;

        materialStore.ClearDirtyMaterials();

        Span<TextureBinding> slots = stackalloc TextureBinding[RenderLimits.TextureSlots];
        foreach (var material in materialStore.MaterialEnumerator())
        {
            int slotLength = materialStore.GetMaterialUploadData(material!, slots, out var payload);
            renderer.SubmitMaterialDrawData(in payload, slots.Slice(0, slotLength));
        }

        _hasUploadedMaterial = true;
    }

    internal void Execute(float delta, float alpha)
    {
        var renderAnimations = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        foreach (var query in Ecs.Game.Query<AnimationComponent, RenderLink>())
        {
            var renderEntity = query.Component2.RenderEntityId;
            if (renderEntity == default) continue;

            var animationPtr = renderAnimations.TryGet(renderEntity);
            if (animationPtr.IsNull) continue;

            ref readonly var a = ref query.Component1;

            if (a.Time < a.PrevTime)
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);
        }
    }
}