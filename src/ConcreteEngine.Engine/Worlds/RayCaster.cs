using System.Numerics;
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


    public RenderEntityId GetEntityByCameraRay(Vector2 screenCoords, out BoundingBox resultBounds, out float distance)
    {
        if (_frameBuffer == null)
        {
            resultBounds = default;
            distance = -1;
            return default;
        }

        ScreenPointToRay(screenCoords, out var ray);

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
    
    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        ScreenPointToRay(screenCoords, out ray);
        return Ray.GetRayPlaneIntersectPoint(in ray, planeY);
    }


    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        if (_terrain == null)
        {
            ray = default;
            return default;
        }

        ScreenPointToRay(screenCoords, out ray);
        return _terrain.GetPointOnTerrainPlane(in ray);
    }

    public void ScreenPointToRay(Vector2 screenCoords, out Ray ray)
    {
        var ndc = CoordinateMath.ToNdcCoords(screenCoords, _camera.Viewport);
        ref readonly var invProjViewMatrix = ref _camera.InverseProjectionViewMatrix;
        VectorMath.UnProject(new Vector3(ndc, -1.0f), in invProjViewMatrix, out var p1); // near
        VectorMath.UnProject(new Vector3(ndc, 1.0f), in invProjViewMatrix, out var p2); // far
        Ray.FromTwoPoints(in p1, in p2, out ray);
    }
    


}