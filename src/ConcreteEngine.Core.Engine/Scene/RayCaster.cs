using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Terrains;
using ConcreteEngine.Core.Engine.Render;

namespace ConcreteEngine.Core.Engine.Scene;

public sealed class RayCaster
{
    private readonly SceneStore _sceneStore;
    private readonly CameraTransform _camera;

    internal RayCaster(SceneStore sceneStore, CameraTransform camera)
    {
        _sceneStore = sceneStore;
        _camera = camera;
    }

    public SceneObject? GetSceneObjectFromView(Vector2 screenCoords, out float distance)
    {
        ScreenPointToRay(screenCoords, out var ray);

        var closestEntity = -1;
        var minDistance = float.MaxValue;
        foreach (var query in RenderWorld.Query.New<WorldBox>().Filter(RenderWorld.MetaStore.VisibleSet))
        {
            if (!_sceneStore.IsLinkedEntity(query.Entity)) continue;

            ref readonly var box = ref query.Component;
            if (CollisionMethods.RayIntersectsBox(in ray, box.Min, box.Max, out var dist) && dist < minDistance)
            {
                minDistance = dist;
                closestEntity = query.Entity;
            }
        }

        if (closestEntity < 0)
        {
            distance = -1;
            return null;
        }

        distance = minDistance;
        return _sceneStore.GetByLinkedEntity(closestEntity);
      
    }

    public Vector3 RaycastEntityOnTerrain(SceneObjectId sceneObjectId, Vector2 mousePos, Vector3 origin)
    {
        if (Terrain.Main == null!) Throwers.InvalidOperation("Terrain is not set");

        var hit = GetPointOnPlane(mousePos, origin.Y, out var ray);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (origin.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = Terrain.Main.GetSmoothHeight(newPoint.X, newPoint.Z);

        //TODO
        ref readonly var bounds = ref _sceneStore.Get(sceneObjectId).Transform.GetBounds();

        newPoint.Y = tHeight - bounds.Min.Y;
        return newPoint;
    }
    
    public bool TryGetPointOnTerrain(Vector2 screenCoords, out Vector3 point)
    {
        if (Terrain.Main == null!)
        {
            point = default;
            return false;
        }

        ScreenPointToRay(screenCoords, out var ray);
        point = Terrain.Main.GetPointOnTerrainPlane(in ray);
        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPointOnPlane(Vector2 screenCoords, float planeY, out Ray ray)
    {
        ScreenPointToRay(screenCoords, out ray);
        return Ray.GetRayPlaneIntersectPoint(in ray, planeY);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScreenPointToRay(Vector2 screenCoords, out Ray ray)
    {
        var ndc = CoordinateMath.ToNdcCoords(screenCoords, EngineWindow.Viewport.Size);
        ref readonly var invProjViewMatrix = ref _camera.InverseProjectionViewMatrix;
        VectorMath.UnProject(new Vector3(ndc, -1.0f), in invProjViewMatrix, out var p1); // near
        VectorMath.UnProject(new Vector3(ndc, 1.0f), in invProjViewMatrix, out var p2); // far
        Ray.FromTwoPoints(p1, p2, out ray);
    }
}