using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Core.Engine;

public sealed class RayCaster
{
    private readonly CameraTransforms _camera;
    private SceneStore _sceneStore= null!;
    private RenderSystem _renderSystem= null!;

    private static Terrain Terrain => Terrain.Main;

    internal RayCaster(CameraTransforms camera)
    {
        _camera = camera;
    }

    internal void Attach(SceneStore sceneStore)
    {
        _sceneStore = sceneStore;
    }

    public SceneObject? GetSceneObjectFromView(Vector2 screenCoords, out BoundingBox resultBounds,
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

        return _sceneStore.Get(new SceneObjectId(Ecs.SceneLink.GetSceneHandleBy(closestEntity), 0));
    }
    
    public Vector3 RaycastEntityOnTerrain(SceneObjectId sceneObjectId, Vector2 mousePos, Vector3 origin)
    {
        if(Terrain == null) Throwers.InvalidOperation("Terrain is not set");
        
        var hit = GetPointOnPlane(mousePos, origin.Y, out var ray);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (origin.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = Terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        ref readonly var bounds = ref _sceneStore.Get(sceneObjectId).Transform.GetBounds();

        newPoint.Y = tHeight - bounds.Min.Y;
        return newPoint;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        ScreenPointToRay(screenCoords, out ray);
        return Ray.GetRayPlaneIntersectPoint(in ray, planeY);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPointOnTerrain(Vector2 screenCoords, out Ray ray)
    {
        if (Terrain == null!)
        {
            ray = default;
            return default;
        }

        ScreenPointToRay(screenCoords, out ray);
        return Terrain.GetPointOnTerrainPlane(in ray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScreenPointToRay(Vector2 screenCoords, out Ray ray)
    {
        var ndc = CoordinateMath.ToNdcCoords(screenCoords, EngineWindow.Current.Viewport.Size);
        ref readonly var invProjViewMatrix = ref _camera.InverseProjectionViewMatrix;
        VectorMath.UnProject(new Vector3(ndc, -1.0f), in invProjViewMatrix, out var p1); // near
        VectorMath.UnProject(new Vector3(ndc, 1.0f), in invProjViewMatrix, out var p2); // far
        Ray.FromTwoPoints(in p1, in p2, out ray);
    }
}