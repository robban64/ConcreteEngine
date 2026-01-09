using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class InteractionApiController(ApiContext apiContext) : EngineInteractionController
{
    private readonly Terrain _terrain = apiContext.World.Terrain;
    private readonly RayCaster _raycaster = apiContext.World.RayCast;
    private readonly SceneStore _sceneStore = apiContext.SceneManager.Store;

    public override Vector3 RaycastTerrain(Vector2 mousePos) => _raycaster.GetPointOnTerrain(mousePos, out _);

    public override SceneObjectId Raycast(Vector2 mousePos)
    {
        var entity = _raycaster.GetEntityByCameraRay(mousePos, out _, out _);
        var sceneObjects = _sceneStore.GetSceneObjectSpan();
        foreach (var sceneObject in sceneObjects)
        {
            if(sceneObject.GetRenderEntities().Contains(entity))
                return sceneObject.Id;
        }
        return default;
    }

    public override Vector3 RaycastEntityOnTerrain(SceneObjectId sceneObjectId, Vector2 mousePos, Vector3 origin)
    {
        var hit = _raycaster.GetPointOnPlane(mousePos, origin.Y, out var ray);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (origin.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = _terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        ref readonly var bounds = ref _sceneStore.Get(sceneObjectId).GetBounds();

        newPoint.Y = tHeight - bounds.Min.Y;
        return newPoint;
    }
}