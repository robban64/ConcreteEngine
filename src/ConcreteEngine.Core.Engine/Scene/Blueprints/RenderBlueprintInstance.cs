using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class RenderBlueprintInstance(SceneObject owner)
{
    public bool IsDirty { get; private set; } = true;

    protected readonly SceneObject Owner = owner;
    protected readonly List<RenderEntityId> RenderEntityIds = [];

    protected BoundingBox WorldBounds;

    public abstract RenderBlueprint GetBlueprint();
    public string DisplayName => GetBlueprint().DisplayName;
    public int EntityCount => RenderEntityIds.Count;
    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);

    public ref readonly BoundingBox GetWorldBounds() => ref WorldBounds;

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

    internal abstract void ApplyTransform(in Matrix4x4 rootMatrix);

    internal void AddEntity() { }

    internal virtual void ApplyMaterial(MaterialState material)
    {
        foreach (var entity in GetRenderEntities())
        {
            var materialId = Ecs.RenderCore.GetSource(entity).Material;
            if (materialId > 0 && materialId != material.MaterialId) continue;
            Ecs.RenderCore.GetDrawPolicy(entity) = new DrawPolicy(material.DrawQueue, material.Passes);
        }
    }

    public void ToggleVisibility(bool visible)
    {
        foreach (var entity in GetRenderEntities())
            Ecs.RenderCore.ToggleVisibility(entity, EntityVisibility.ForceHidden, visible);
    }

    public void ToggleSelection(bool isSelected)
    {
        if (isSelected)
        {
            foreach (var entity in GetRenderEntities())
                Ecs.GetRenderStore<SelectionComponent>().Add(entity, SelectionComponent.DefaultHighlight);
        }
        else
        {
            foreach (var entity in GetRenderEntities())
                Ecs.GetRenderStore<SelectionComponent>().Remove(entity);
        }
        
        Ecs.GetRenderStore<SelectionComponent>().Commit();
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
        Ecs.GetRenderStore<SelectionComponent>().Commit();
    }
}