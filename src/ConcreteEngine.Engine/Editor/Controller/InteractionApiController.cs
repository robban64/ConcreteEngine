using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class InteractionApiController(ApiContext apiContext) : InteractionController
{
    private readonly Terrain _terrain = apiContext.World.Terrain;
    private readonly SceneStore _sceneStore = apiContext.SceneManager.Store;

    private RayCaster Raycaster => CameraSystem.Instance.RayCaster;

    public override Vector3 RaycastTerrain(Vector2 mousePos) => Raycaster.GetPointOnTerrain(mousePos, out _);

    private Stopwatch sw = new Stopwatch();
    public override SceneObjectId Raycast(Vector2 mousePos)
    {
        sw.Start();
        var sceneObject = Raycaster.GetSceneObjectByCameraRay(mousePos, out _, out _);
        sw.Stop();
        Console.WriteLine($"{sw.ElapsedTicks / 1000.0:F4}");
        sw.Reset();
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
        var tHeight = _terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        ref readonly var bounds = ref _sceneStore.Get(sceneObjectId).GetBounds();

        newPoint.Y = tHeight - bounds.Min.Y;
        return newPoint;
    }
}