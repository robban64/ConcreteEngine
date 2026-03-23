using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine;

public sealed class RayCaster
{
    private readonly Camera _camera;
    private Terrain? _terrain;
    private SceneManager? _sceneManager;
    private EngineRenderSystem _renderSystem;

    internal RayCaster(Camera camera)
    {
        _camera = camera;
    }

    internal void Attach(SceneManager sceneManager, EngineRenderSystem renderSystem)
    {
        _sceneManager = sceneManager;
        _terrain = renderSystem.Terrain.Terrain;
        _renderSystem = renderSystem;
    }

    public SceneObject? GetSceneObjectByCameraRay(Vector2 screenCoords, out BoundingBox resultBounds,
        out float distance)
    {
        ScreenPointToRay(screenCoords, out var ray);

        distance = float.MaxValue;
        resultBounds = default;

        var ecs = Ecs.Render.Core;
        RenderEntityId closestEntity = default;
        foreach (var entity in _renderSystem.VisibleEntities())
        {
            ref readonly var box = ref ecs.GetBounds(entity);
            ref readonly var matrix = ref ecs.GetParentMatrix(entity);
            BoundingBox.GetWorldBounds(in box, in matrix, out var worldBounds);
            if (CollisionMethods.RayIntersectsBox(in ray, in worldBounds, out var dist) && dist < distance)
            {
                distance = dist;
                resultBounds = worldBounds;
                closestEntity = entity;
            }
        }

        if (!closestEntity.IsValid()) return null;

        return _sceneManager!.Store.Get(new SceneObjectId(Ecs.SceneLink.GetSceneHandleBy(closestEntity), 0));
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