using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.Scene;


public abstract class RenderBlueprintInstance(SceneObject owner)
{
    
    protected readonly SceneObject Owner = owner;
    public bool IsDirty { get; private set; } = true;
    
    protected readonly List<RenderEntityId> RenderEntityIds = [];
    
    //public Transform LocalTransform;
    public BoundingBox WorldBounds;

    public abstract RenderBlueprint GetBlueprint();
    public string DisplayName => GetBlueprint().DisplayName;
    public int EntityCount => RenderEntityIds.Count;
    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);
    
    internal void MarkDirty(SceneDirtyFlags flag)
    {
        IsDirty = true;
        Owner.MarkDirty(flag);
    }

    internal void Commit()
    {
        IsDirty = false;
        OnCommit();
    }

    internal abstract void OnCreate();
    protected virtual void OnCommit() { }
    
    internal abstract void ApplyTransform();

    internal void AddEntity()
    {
        
    }

    internal virtual void ApplyMaterial(MaterialState material)
    {
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.Render.Core.GetSource(entity);
            if (source.Material.Id > 0 && source.Material != material.MaterialId) continue;
            source.Queue = material.DrawQueue;
            source.Passes = material.PassMasks;
        }
    }

    public void ToggleSelection(bool isSelected)
    {
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
