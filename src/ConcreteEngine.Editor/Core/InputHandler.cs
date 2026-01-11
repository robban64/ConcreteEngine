using System.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class InputHandler(GlobalContext ctx)
{
    public void OnRightClickViewport()
    {
        ctx.TriggerStateEvent<SceneState, SceneObjectId>(EventKey.SelectionChanged, SceneObjectId.Empty);
    }

    public bool OnClickViewport(Vector2 mousePos)
    {
        var sceneObjectId = ctx.InteractionController.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (ctx.SelectedId.IsValid())
                ctx.TriggerStateEvent<SceneState, SceneObjectId>(EventKey.SelectionChanged, SceneObjectId.Empty);

            return false;
        }

        if (sceneObjectId.Id == ctx.SelectedId) return true;

        ctx.TriggerStateEvent<SceneState, SceneObjectId>(EventKey.SelectionChanged, sceneObjectId);
        return true;
    }

    public bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = ctx.InteractionController.RaycastTerrain(mousePos);
        return point != default;
    }

    public void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var newPos = ctx.InteractionController.RaycastEntityOnTerrain(ctx.SelectedId, mousePos, origin);
        if (newPos == default) return;

        if (ctx.SelectedProxy is { } proxy)
        {
            var property = proxy.GetSpatialProperty();
            var spatial = property.GetValue();
            spatial.Transform.Translation = newPos;
            proxy.GetSpatialProperty().SetValue(spatial);
        }

        //EngineController.CommitSceneObject();
    }
}