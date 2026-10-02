using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class RenderBlueprintInstance(SceneObject owner)
{
    public bool IsDirty { get; private set; } = true;

    protected readonly SceneObject Owner = owner;
    protected readonly List<RenderEntity> RenderEntityIds = [];

    protected BoundingAxisBox WorldBounds;

    public abstract RenderBlueprint GetBlueprint();
    public string DisplayName => GetBlueprint().DisplayName;
    public int EntityCount => RenderEntityIds.Count;
    public ReadOnlySpan<RenderEntity> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);

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
            var ctx = RenderEcs.Core.GetContext(entity);
            var materialId = ctx.Source.Material;
            if (materialId > 0 && materialId != material.MaterialId) continue;
            ctx.Policy = new DrawPolicy(material.DrawQueue, material.Passes);
        }
    }

    public void ToggleVisibility(bool visible)
    {
        var flag = visible ? EntityDrawStatus.Normal : EntityDrawStatus.ForceHidden;
        foreach (var entity in GetRenderEntities())
        {
            var ctx = RenderEcs.Core.GetContext(entity);
            ctx.SetStatus(flag);
        }
    }

    public void ToggleSelection(bool isSelected)
    {
        if (isSelected)
        {
            foreach (var entity in GetRenderEntities())
            {
                var ctx = RenderEcs.Core.GetContext(entity);
                ctx.SetStatus(EntityDrawStatus.ForceHidden);
                ctx.AddComponent(SelectionComponent.DefaultHighlight);
            }
        }
        else
        {
            foreach (var entity in GetRenderEntities())
            {
                var ctx = RenderEcs.Core.GetContext(entity);
                ctx.SetStatus(EntityDrawStatus.Normal);
                ctx.RemoveComponent<SelectionComponent>();
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