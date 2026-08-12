using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class RenderBlueprintInstance(SceneObject owner)
{
    public bool IsDirty { get; private set; } = true;

    protected readonly SceneObject Owner = owner;
    protected readonly List<RenderEntityId> RenderEntityIds = [];

    protected BoundingAxisBox WorldBounds;

    public abstract RenderBlueprint GetBlueprint();
    public string DisplayName => GetBlueprint().DisplayName;
    public int EntityCount => RenderEntityIds.Count;
    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);

    public ref readonly BoundingAxisBox GetWorldBounds() => ref WorldBounds;

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
            var materialId = RenderEcs.Core.GetSource(entity).Material;
            if (materialId > 0 && materialId != material.MaterialId) continue;
            RenderEcs.Core.GetDrawPolicy(entity) = new DrawPolicy(material.DrawQueue, material.Passes);
        }
    }

    public void ToggleVisibility(bool visible)
    {
        var flag = visible ? EntityDrawStatus.Normal : EntityDrawStatus.ForceHidden;
        foreach (var entity in GetRenderEntities()) RenderEcs.Core.SetStatus(entity, flag);
    }

    public void ToggleSelection(bool isSelected)
    {
        if (isSelected)
        {
            foreach (var entity in GetRenderEntities())
            {
                RenderEcs.Store<SelectionComponent>().Add(entity, SelectionComponent.DefaultHighlight);
                RenderEcs.Core.SetStatus(entity, EntityDrawStatus.ForceHidden);
            }
        }
        else
        {
            foreach (var entity in GetRenderEntities())
            {
                RenderEcs.Store<SelectionComponent>().Remove(entity);
                RenderEcs.Core.SetStatus(entity, EntityDrawStatus.Normal);
            }
        }
        
        RenderEcs.Store<SelectionComponent>().Commit();
    }

    public void ToggleDebugBounds(bool isSelected)
    {
        var debugStore = RenderEcs.Store<DebugBoundsComponent>();
        var span = GetRenderEntities();
        for (var i = 0; i < span.Length; i++)
        {
            var entity = span[i];
            var color = DebugBoundsComponent.DefaultColors[i % (DebugBoundsComponent.DefaultColors.Length - 1)];
            if (isSelected) debugStore.Add(entity, new DebugBoundsComponent(color));
            else debugStore.Remove(entity);
        }
        debugStore.Commit();
    }
}