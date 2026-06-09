using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintInstance
{
    public abstract SceneObjectBlueprint GetBlueprint();

    public bool IsDirty { get; internal set; } = true;

    internal readonly List<RenderEntityId> RenderEntityIds = [];
    internal readonly List<GameEntityId> GameEntityIds = [];

    public string DisplayName => GetBlueprint().DisplayName;

    public bool HasRenderEcs => RenderEntityIds.Count > 0;
    public bool HasGameEntityIds => GameEntityIds.Count > 0;
    public bool IsMixedEcs => HasRenderEcs && HasGameEntityIds;

    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(GameEntityIds);

    internal void Commit(SceneObject sceneObject)
    {
        IsDirty = false;
        OnCommit(sceneObject);
    }

    protected virtual void OnCommit(SceneObject sceneObject) { }

    // TODO
    public void ToggleSelection(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var selectionStore = Ecs.GetRenderStore<SelectionComponent>();

        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.RenderCore.GetSource(entity);
            if (isSelected)
            {
                selectionStore.Add(entity, new SelectionComponent(SelectionComponent.DefaultHighlight, source.Passes));
            }
            else
            {
                var passes = selectionStore.Get(entity).OriginalPasses;
                source = source with { Resolver = 0, ResolverSlot = 0, Passes = passes };
                selectionStore.Remove(entity);
            }
        }
    }

    public void ToggleDebugBounds(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var debugStore = Ecs.Render.Stores<DebugBoundsComponent>.Store;
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
