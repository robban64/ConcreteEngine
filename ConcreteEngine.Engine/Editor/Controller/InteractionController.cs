#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class InteractionController(ApiContext apiContext)
{

    private WorldRaycaster Raycaster => apiContext.World.Raycast;
    private DragEntityState _dragState;
    
    public bool IsDragging => _dragState.IsDragging;

    private ref BoxComponent GetBounds(EntityId e) =>
        ref apiContext.World.Entities.Core.GetBoxById(e);


    public EntityId OnClick(Vector2 mousePosition, out BoundingBox bounds, out float distance)
    {
        var entity = Raycaster.GetEntityByCameraRay(mousePosition, out bounds, out distance);
        if (entity == default) return default;

        return entity;
    }

    public void OnDragEntity(EntityId entity, Vector2 mousePosition)
    {
        if (!_dragState.IsDragging && !_dragState.WasDragging)
            OnDragStart(mousePosition);
        else if (!_dragState.IsDragging && _dragState.WasDragging)
            OnDragEnd(mousePosition);
        else if (_dragState.IsDragging)
            OnDragUpdate(entity, mousePosition);
        else
            OnDragEnd(mousePosition);
    }

    private void OnDragStart( Vector2 mousePosition)
    {
        var world = apiContext.World;

        var pointOnTerrain = world.Raycast.GetPointOnTerrain(mousePosition, out _);
        if (pointOnTerrain == default) return;
        _dragState.DragStart = pointOnTerrain;
        _dragState.IsDragging = true;
        _dragState.WasDragging = false;
    }

    private void OnDragUpdate(EntityId entity, Vector2 mousePosition)
    {
        var world = apiContext.World;
        world.Camera.Raycaster.CreateRayFrom(mousePosition, out var ray);

        var hit = GetRayPlaneIntersectPoint(in ray, _dragState.DragStart.Y);
        if (hit == default) return;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return;

        float t = (_dragState.DragStart.Y - ray.Position.Y) / denom;
        if (t < 0) return;

        ref var transform = ref world.Entities.Core.GetTransformById(entity);
        ref readonly var bounds = ref GetBounds(entity);

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = world.Terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        newPoint.Y = tHeight - bounds.Bounds.Min.Y;
        transform.Translation = newPoint;

        _dragState.IsDragging = true;
        _dragState.WasDragging = true;
    }

    private void OnDragEnd(Vector2 mousePosition)
    {
        _dragState = default;
    }

    private Vector3 DragEntityTerrain(EntityId entity,Vector2 mousePosition)
    {
        var world = apiContext.World;
        world.Camera.Raycaster.CreateRayFrom(mousePosition, out var ray);

        var hit = GetRayPlaneIntersectPoint(in ray, _dragState.DragStart.Y);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (_dragState.DragStart.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        ref readonly var bounds = ref GetBounds(entity);

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = world.Terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        newPoint.Y = tHeight - bounds.Bounds.Min.Y;
        return newPoint;
    }

    private struct DragEntityState
    {
        public Vector3 DragStart;
        public bool IsDragging;
        public bool WasDragging;
    }

    private static Vector3 GetRayPlaneIntersectPoint(in Ray ray, float planeY)
    {
        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return Vector3.Zero;
        float t = (planeY - ray.Position.Y) / denom;

        return t < 0 ? Vector3.Zero : ray.GetPointOnRay(t);
    }
}