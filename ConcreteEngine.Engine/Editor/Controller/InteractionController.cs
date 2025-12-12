#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class InteractionController(ApiContext apiContext) : IEngineInteractionController
{
    private readonly WorldTerrain _terrain = apiContext.World.Terrain;
    private readonly WorldRaycaster _raycaster = apiContext.World.Raycast;
    private readonly WorldEntities _entities = apiContext.World.Entities;


    public Vector3 RaycastTerrain(Vector2 mousePos) => _raycaster.GetPointOnTerrain(mousePos, out _);

    public EditorId Raycast(Vector2 mousePos)
    {
        var entity = _raycaster.GetEntityByCameraRay(mousePos, out _, out _);
        return entity != default ? new EditorId(entity, EditorItemType.Entity) : default;
    }

    public Vector3 RaycastEntityOnTerrain(EditorId entity, Vector2 mousePos, Vector3 origin)
    {
        var hit = _raycaster.GetPointOnPlane(mousePos, origin.Y, out var ray);
        if (hit == default) return default;

        float denom = ray.Direction.Y;
        if (Math.Abs(denom) < 1e-6f) return default;

        float t = (origin.Y - ray.Position.Y) / denom;
        if (t < 0) return default;

        var newPoint = ray.GetPointOnRay(t);
        var tHeight = _terrain.GetSmoothHeight(newPoint.X, newPoint.Z);

        var entityId = new EntityId(entity);
        ref readonly var bounds = ref _entities.Core.GetBoxById(entityId);

        newPoint.Y = tHeight - bounds.Bounds.Min.Y;
        return newPoint;
    }
}