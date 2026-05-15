using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintInstance(SceneObjectBlueprint blueprint)
{
    public virtual SceneObjectBlueprint Blueprint { get; } = blueprint;
    
    public string DisplayName { get; set; } = blueprint.DisplayName;
    public bool IsDirty { get; private set; } = true;

    internal readonly List<RenderEntityId> RenderEntityIds = [];
    internal readonly List<GameEntityId> GameEntityIds = [];

    public bool HasRenderEcs => RenderEntityIds.Count > 0;
    public bool HasGameEntityIds => GameEntityIds.Count > 0;
    public bool IsMixedEcs => HasRenderEcs && HasGameEntityIds;

    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(GameEntityIds);

    internal virtual void Commit() => IsDirty = false;

    public void ToggleSelection(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var selectionStore = Ecs.Render.Stores<SelectionComponent>.Store;

        foreach (var entity in GetRenderEntities())
        {
            if (isSelected) selectionStore.Add(entity, new SelectionComponent(SelectionComponent.DefaultHighlight));
            else selectionStore.Remove(entity);
        }
    }

    public void ToggleDebugBounds(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var debugStore = Ecs.Render.Stores<DebugBoundsComponent>.Store;
        var isFirst = true;

        var span = GetRenderEntities();
        for (var i = 0; i < span.Length; i++)
        {
            var entity = span[i];
            var color = DebugBoundsComponent.DefaultColors[i % (DebugBoundsComponent.DefaultColors.Length - 1)];
            if (isSelected) debugStore.Add(entity, new DebugBoundsComponent(color));
            else debugStore.Remove(entity);

        }
    }
}

public abstract class BlueprintInstance<TBlueprint>(TBlueprint blueprint) : BlueprintInstance(blueprint)
    where TBlueprint : SceneObjectBlueprint
{
    public override TBlueprint Blueprint { get; } = blueprint;
}

public sealed class ModelInstance(ModelBlueprint blueprint, Model asset)
    : BlueprintInstance<ModelBlueprint>(blueprint)
{
    public Model Asset { get; } = asset;
    public readonly List<Material> Materials = new(asset.Meshes.Length);

    public Transform LocalTransform = blueprint.LocalTransform;
    public BoundingBox LocalBounds = asset.Bounds;
}

public sealed class AnimationInstance(ModelBlueprint blueprint, ModelAnimation assetAnimation)
    : BlueprintInstance<ModelBlueprint>(blueprint)
{
    public ModelAnimation AssetAnimation { get; } = assetAnimation;

    public ref AnimationComponent GetComponent()
    {
        var store = Ecs.Game.Stores<AnimationComponent>.Store;
        foreach (var entity in GameEntityIds)
        {
            if (store.Has(entity)) return ref store.Get(entity);
        }

        throw new InvalidOperationException();
    }
}

public sealed class ParticleInstance(ParticleBlueprint blueprint, ParticleEmitter emitter)
    : BlueprintInstance<ParticleBlueprint>(blueprint)
{
    public ParticleEmitter Emitter { get; } = emitter;
}