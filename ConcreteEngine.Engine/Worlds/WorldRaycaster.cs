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

    internal WorldRaycaster(Camera3D camera, WorldEntities entities)
    {
        _camera = camera;
        _entities = entities;
    }

    public EntityId GetEntityByCameraRay(Vector2 screenCoords, out float distance)
    {
        var ray = _camera.Raycaster.CreateRayFrom(screenCoords);

        Span<Vector3> corners = stackalloc Vector3[8];

        Matrix4x4 world = default;
        BoundingAxisBox axisBounds = default;
        BoundingBox finalBounds = default;

        distance = 0;

        foreach (var it in _entities.Query<Transform, BoxComponent>())
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
                return it.Entity;
            }
        }

        return default;
    }
}