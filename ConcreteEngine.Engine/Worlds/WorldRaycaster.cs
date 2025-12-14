#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.View;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldRaycaster
{
    private readonly Camera3D _camera;
    private readonly WorldEntities _entities;
    private readonly WorldTerrain _terrain;
    private RenderEntityBus _renderEntities = null!;

    private CameraRaycaster CameraRaycaster => _camera.Raycaster;

    internal WorldRaycaster(Camera3D camera, WorldEntities entities, WorldTerrain terrain)
    {
        _entities = entities;
        _terrain = terrain;
        _camera = camera;
    }

    internal void AttachRenderer(RenderEntityBus renderEntities) => _renderEntities = renderEntities;

    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        CameraRaycaster.CreateRayFrom(screenCoords, out ray);
        return GetRayPlaneIntersectPoint(in ray, planeY);
    }


    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        CameraRaycaster.CreateRayFrom(screenCoords, out ray);
        return _terrain.GetPointOnTerrainPlane(in ray);
    }

    public EntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox resultBounds, out float distance)
    {
        CameraRaycaster.CreateRayFrom(screenCoords, out var ray);

        distance = float.MaxValue;
        resultBounds = default;

        var visibleEntities = _renderEntities.VisibleEntities;
        if (visibleEntities.Length == 0) return default;
        var coreView = _entities.Core.GetCoreView();

        EntityId closestEntity = default;
        BoundingBox worldBounds;
        foreach (var entity in visibleEntities)
        {
            ref readonly var transform = ref coreView.GetTransform(entity);
            ref readonly var box = ref coreView.GetBox(entity);
            RenderTransform.GetWorldBounds(in box.Bounds, in transform, out worldBounds);
            if (CollisionMethods.RayIntersectsBox(in ray, in worldBounds, out var dist) && dist < distance)
            {
                distance = dist;
                closestEntity = entity;
                resultBounds = worldBounds;
            }
        }

        return closestEntity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetRayPlaneIntersectPoint(in Ray ray, float planeY)
    {
        float denom = ray.Direction.Y;
        if (float.Abs(denom) < 1e-6f) return default;
        float t = (planeY - ray.Position.Y) / denom;

        return t < 0 ? default : ray.GetPointOnRay(t);
    }
}