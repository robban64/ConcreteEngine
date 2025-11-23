using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.View;

namespace ConcreteEngine.Engine.Worlds;

public sealed class WorldRaycaster
{
    private readonly Camera3D _camera;
    private readonly WorldEntities _entities;
    private readonly WorldTerrain _terrain;

    internal WorldRaycaster(Camera3D camera, WorldEntities entities, WorldTerrain terrain)
    {
        _camera = camera;
        _entities = entities;
        _terrain = terrain;
    }

    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        _camera.Raycaster.CreateRayFrom(screenCoords, out ray);
        return _terrain.GetPointOnTerrainPlane(in ray);
    }

    public EntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox entityBounds, out float distance)
    {
        _camera.Raycaster.CreateRayFrom(screenCoords, out var ray);

        Span<Vector3> corners = stackalloc Vector3[8];

        Matrix4x4 world = default;
        BoundingAxisBox axisBounds = default;
        BoundingBox finalBounds = default;

        distance = 0;

        foreach (var it in _entities.Query<TransformComponent, BoxComponent>())
        {
            ref readonly var transform = ref it.Component1;
            ref readonly var bounds = ref it.Component2;

            MatrixMath.CreateModelMatrix(in transform.Translation, in transform.Scale,
                in transform.Rotation, out world);

            bounds.Box.FillCorners(corners);

            for (var i = 0; i < corners.Length; i++)
                corners[i] = Vector3.Transform(corners[i], world);

            BoundingAxisBox.FromPoints(corners, out axisBounds);
            BoundingBox.FromAxisBox(in axisBounds, out finalBounds);
            if (ray.IntersectsWith(in finalBounds, out distance))
            {
                entityBounds = finalBounds;
                return it.Entity;
            }
        }

        entityBounds = default;
        return default;
    }
}