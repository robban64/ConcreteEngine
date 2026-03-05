using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Worlds;

public sealed class RayCaster
{
    private readonly CameraTransform _camera;
    private Terrain? _terrain;
    private FrameEntityBuffer? _frameBuffer;

    internal RayCaster(CameraTransform camera)
    {
        _camera = camera;
    }

    internal void Attach(Terrain terrain, FrameEntityBuffer frameBuffer)
    {
        _terrain = terrain;
        _frameBuffer = frameBuffer;
    }

    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        CreateRayFrom(screenCoords, out ray);
        return GetRayPlaneIntersectPoint(in ray, planeY);
    }


    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        if (_terrain == null)
        {
            ray = default;
            return default;
        }
        CreateRayFrom(screenCoords, out ray);
        return _terrain.GetPointOnTerrainPlane(in ray);
    }

    public RenderEntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox resultBounds, out float distance)
    {
        if (_frameBuffer == null)
        {
            resultBounds = default;
            distance = -1;
            return default;
        }
        
        CreateRayFrom(screenCoords, out var ray);

        distance = float.MaxValue;
        resultBounds = default;

        var visibleEntities = _frameBuffer.GetVisibleEntities();
        if (visibleEntities.Length == 0) return default;
        var renderEcs = Ecs.Render.Core;

        RenderEntityId closestEntity = default;
        BoundingBox worldBounds;
        foreach (var entity in visibleEntities)
        {
            ref readonly var box = ref renderEcs.GetBox(entity);
            ref readonly var matrix = ref renderEcs.GetParentMatrix(entity);

            BoundingBox.GetWorldBounds(in box, in matrix, out worldBounds);
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