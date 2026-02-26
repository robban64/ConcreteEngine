using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class EditorEventHandler(StateContext ctx, EngineController controller)
{
    public void OnSceneObjectEvent(SceneObjectEvent evt)
    {
        if (ctx.Selection.SelectedSceneId == evt.SceneObject) return;
        if (!evt.SceneObject.IsValid())
        {
            ctx.Selection.DeSelectSceneObject();
            ctx.EmitTransition(TransitionMessage.PopRight());
        }

        ctx.Selection.SelectSceneObject(evt.SceneObject);
        ctx.EmitTransition(TransitionMessage.PushRight(PanelId.SceneProperty));
    }

    public void OnAssetSelectionEvent(AssetSelectionEvent evt)
    {
        if (ctx.Selection.SelectedAssetId == evt.Asset) return;

        if (!evt.Asset.IsValid())
        {
            ctx.Selection.DeselectAsset();
            ctx.EmitTransition(TransitionMessage.PopRight());
            return;
        }

        ctx.Selection.SelectAsset(evt.Asset);
        ctx.EmitTransition(TransitionMessage.PushRight(PanelId.AssetProperty));
    }

    public static void OnAssetUpdateEvent(AssetUpdateEvent evt)
    {
        var action = evt.Action switch
        {
            AssetUpdateEvent.EventAction.Reload => CommandAssetAction.Reload,
            _ => throw new ArgumentOutOfRangeException()
        };


        CommandDispatcher.InvokeEditorCommand(new AssetCommandRecord(action, evt.Asset));
    }
}