using System.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class InputHandler(GlobalContext ctx)
{
    public void OnRightClickViewport()
    {
        if (ctx.Selection.SelectedSceneId.IsValid())
            ctx.TriggerStateEvent<SceneComponent, SceneObjectId>(EventKey.SelectionChanged, SceneObjectId.Empty);
    }

    public bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = ctx.Selection.SelectedSceneId;
        var sceneObjectId = EngineController.InteractionController.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                ctx.TriggerStateEvent<SceneComponent, SceneObjectId>(EventKey.SelectionChanged, SceneObjectId.Empty);

            return false;
        }

        if (sceneObjectId.Id == selectedId) return true;
        ctx.TriggerStateEvent<SceneComponent, SceneObjectId>(EventKey.SelectionChanged, sceneObjectId);

        return true;
    }

    public bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = EngineController.InteractionController.RaycastTerrain(mousePos);
        return point != default;
    }

    public void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = ctx.Selection.SelectedSceneId;
        var newPos = EngineController.InteractionController.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || ctx.Selection.SceneProxy is not { } proxy) return;

        var property = proxy.GetSpatialProperty();
        if (property is null) return;
        var transform = property.Get();
        transform.Transform.Translation = newPos;
        property.Set(in transform);
    }
}