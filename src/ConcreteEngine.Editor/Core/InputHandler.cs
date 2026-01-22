using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class InputHandler(InteractionController interaction, StateContext ctx)
{
    public void OnRightClickViewport()
    {
        if (ctx.Selection.SelectedSceneId.IsValid())
            ctx.EnqueueEvent(new SceneObjectEvent(SceneObjectId.Empty));
    }

    public bool OnClickViewport(Vector2 mousePos)
    {
        var selectedId = ctx.Selection.SelectedSceneId;
        var sceneObjectId = interaction.Raycast(mousePos);
        if (!sceneObjectId.IsValid())
        {
            if (selectedId.IsValid())
                ctx.EnqueueEvent(new SceneObjectEvent(SceneObjectId.Empty));

            return false;
        }

        if (sceneObjectId.Id == selectedId) return true;

        ctx.EnqueueEvent(new SceneObjectEvent(sceneObjectId));

        return true;
    }

    public bool RaycastTerrain(Vector2 mousePos, out Vector3 point)
    {
        point = interaction.RaycastTerrain(mousePos);
        return point != default;
    }

    public void OnDragTerrain(Vector2 mousePos, Vector3 origin)
    {
        var id = ctx.Selection.SelectedSceneId;
        var newPos = interaction.RaycastEntityOnTerrain(id, mousePos, origin);
        if (newPos == default || ctx.Selection.SceneProxy is not { } proxy) return;

        var property = proxy.Properties.SpatialProperty;
        property.Transform.Translation = newPos;
        property.InvokeSet();
    }
}