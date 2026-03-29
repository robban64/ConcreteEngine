using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class EditorEventHandler(StateContext ctx)
{
    public void OnSelectionEvent(SelectionEvent evt)
    {
        if (evt.Asset == null && evt.SceneObject == null)
            throw new ArgumentException("Either Asset or SceneObject must be set");

        if (evt.Asset is { } asset)
        {
            if (ctx.Selection.SelectedAssetId == asset) return;
            if (!asset.IsValid())
            {
                ctx.Selection.DeselectAsset();
                ctx.EmitTransition(TransitionMessage.PopRight());
                return;
            }

            ctx.Selection.SelectAsset(asset);
            ctx.EmitTransition(TransitionMessage.PushRight(PanelId.AssetInspector));
            return;
        }

        if (evt.SceneObject is { } sceneObject)
        {
            if (ctx.Selection.SelectedSceneId == sceneObject) return;
            if (!sceneObject.IsValid())
            {
                ctx.Selection.DeSelectSceneObject();
                ctx.EmitTransition(TransitionMessage.PopRight());
                return;
            }

            ctx.Selection.SelectSceneObject(sceneObject);
            ctx.EmitTransition(TransitionMessage.PushRight(PanelId.SceneInspector));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnSceneObjectEvent(SceneObjectEvent evt)
    {
        switch (evt.Action)
        {
            case EditorEvent.EventAction.Rename:
                ArgumentException.ThrowIfNullOrWhiteSpace(evt.Name);
                var asset = EngineObjectStore.SceneController.GetSceneObject(evt.SceneObject);
                asset.SetName(evt.Name);
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void OnAssetUpdateEvent(AssetEvent evt)
    {
        switch (evt.Action)
        {
            case EditorEvent.EventAction.Rename:
                ArgumentException.ThrowIfNullOrWhiteSpace(evt.Name);
                var asset = EngineObjectStore.AssetProvider.GetAsset(evt.Asset);
                asset.SetName(evt.Name);
                break;
            case EditorEvent.EventAction.Reload:
                CommandDispatcher.InvokeEditorCommand(new AssetCommandRecord(CommandAssetAction.Reload, evt.Asset));
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}