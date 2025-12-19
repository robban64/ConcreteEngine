using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldRaycaster
{
    private readonly Camera3D _camera;
    private readonly RenderEntityHub _entities;
    private readonly WorldTerrain _terrain;
    private readonly DrawEntityAssembler _drawEntities;

    internal WorldRaycaster(Camera3D camera, RenderEntityHub entities, WorldTerrain terrain,
        DrawEntityAssembler drawEntities)
    {
        _entities = entities;
        _terrain = terrain;
        _camera = camera;
        _drawEntities = drawEntities;
    }


    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        CreateRayFrom(screenCoords, out ray);
        return GetRayPlaneIntersectPoint(in ray, planeY);
    }


    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        CreateRayFrom(screenCoords, out ray);
        return _terrain.GetPointOnTerrainPlane(in ray);
    }

    public RenderEntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox resultBounds, out float distance)
    {
        CreateRayFrom(screenCoords, out var ray);

        distance = float.MaxValue;
        resultBounds = default;

        var visibleEntities = _drawEntities.VisibleEntities;
        if (visibleEntities.Length == 0) return default;
        var coreView = _entities.Core.GetContext();

        RenderEntityId closestEntity = default;
        BoundingBox worldBounds;
        foreach (var entity in visibleEntities)
        {
            ref readonly var transform = ref coreView.GetTransform(entity).Transform;
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

    private void CreateRayFrom(Vector2 screenCoords, out Ray ray)
    {
        var ndc = CoordinateMath.ToNdcCoords(screenCoords, _camera.Viewport);
        UnProject(new Vector3(ndc, -1.0f), in _camera.InverseProjectionViewMatrix, out var p1); // near
        UnProject(new Vector3(ndc, 1.0f), in _camera.InverseProjectionViewMatrix, out var p2); // far
        Ray.FromTwoPoints(in p1, in p2, out ray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 GetRayPlaneIntersectPoint(in Ray ray, float planeY)
    {
        float denom = ray.Direction.Y;
        if (float.Abs(denom) < 1e-6f) return default;
        float t = (planeY - ray.Position.Y) / denom;

        return t < 0 ? default : ray.GetPointOnRay(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UnProject(in Vector3 mouseNdc, in Matrix4x4 invViewProjection, out Vector3 point)
    {
        var vec = new Vector4(mouseNdc, 1.0f);
        vec = Vector4.Transform(vec, invViewProjection);

        if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
        {
            vec.X /= vec.W;
            vec.Y /= vec.W;
            vec.Z /= vec.W;
        }

        point = new Vector3(vec.X, vec.Y, vec.Z);
    }
}