using System.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.TerrainV2;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class InteractionApiController(SceneManager sceneManager) : InteractionController
{
    private readonly TerrainNew _terrain = TerrainManager.Instance.Terrain;
    private readonly SceneStore _sceneStore = sceneManager.Store;

    private RayCaster Raycaster => CameraManager.Instance.RayCaster;

    public override Vector3 RaycastTerrain(Vector2 mousePos) => Raycaster.GetPointOnTerrain(mousePos, out _);

    public override SceneObjectId Raycast(Vector2 mousePos)
    {
        var sceneObject = Raycaster.GetSceneObjectByCameraRay(mousePos, out _, out _);
        return sceneObject?.Id ?? SceneObjectId.Empty;
    }


    public override Vector3 RaycastEntityOnTerrain(SceneObjectId sceneObjectId, Vector2 mousePos, Vector3 origin)
    {
        var hit = Raycaster.GetPointOnPlane(mousePos, origin.Y, out var ray);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (origin.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = _terrain.GetGlobalHeight(newPoint.X, newPoint.Y);
        //var tHeight = _terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        ref readonly var bounds = ref _sceneStore.Get(sceneObjectId).GetBounds();

        newPoint.Y = tHeight - bounds.Min.Y;
        return newPoint;
    }
}