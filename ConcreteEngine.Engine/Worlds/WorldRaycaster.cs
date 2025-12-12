#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.View;

#endregion

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldRaycaster
{

    private readonly Camera3D _camera;
    private readonly WorldEntities _entities;
    private readonly WorldTerrain _terrain;

    private CameraRaycaster CameraRaycaster => _camera.Raycaster;

    internal WorldRaycaster(Camera3D camera, WorldEntities entities, WorldTerrain terrain)
    {
        _entities = entities;
        _terrain = terrain;
        _camera = camera;
    }
    
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

    // TODO lol
    public EntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox entityBounds, out float distance)
    {
        CameraRaycaster.CreateRayFrom(screenCoords, out var ray);

        Span<Vector3> corners = stackalloc Vector3[8];

        Matrix4x4 world;
        BoundingAxisBox axisBounds;
        BoundingBox finalBounds;

        distance = 0;

        var core = _entities.Core;
        foreach (var query in _entities.CoreQuery())
        {
            var entity = query.Entity;
            ref readonly var transform = ref core.GetTransformById(entity);
            ref readonly var box = ref query.Box;

            MatrixMath.CreateModelMatrix(in transform.Translation, in transform.Scale,
                in transform.Rotation, out world);

            box.Bounds.FillCorners(corners);

            for (var c = 0; c < corners.Length; c++)
                corners[c] = Vector3.Transform(corners[c], world);

            BoundingAxisBox.FromPoints(corners, out axisBounds);
            BoundingBox.FromAxisBox(in axisBounds, out finalBounds);
            if (ray.IntersectsWith(in finalBounds, out distance))
            {
                entityBounds = finalBounds;
                return entity;
            }
        }

        entityBounds = default;
        return default;
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