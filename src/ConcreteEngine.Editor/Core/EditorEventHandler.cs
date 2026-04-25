using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI.Assets;

namespace ConcreteEngine.Editor.Core;

internal static class EditorEventHandler
{
    public static void OnModeEvent(ModeEvent evt, StateManager ctx)
    {
        ctx.EmitChange(ctx.Context with { Mode = new ModeContext { IsMetricMode = evt.MetricMode } });
    }

    public static void OnToolEvent(ToolEvent evt, StateManager ctx)
    {
        if (evt.ShowDebugBounds is null && evt.GizmoEnabled is null)
            throw new ArgumentException();

        var tool = ctx.Context.Tool;

        if (evt.GizmoEnabled is { } gizmoEnabled)
        {
            tool = tool with { GizmoEnabled = gizmoEnabled, GizmoMode = evt.GizmoMode, GizmoOp = evt.GizmoOperation };
        }

        if (evt.ShowDebugBounds is { } showDebugBounds)
        {
            tool = tool with { ShowDebugBounds = showDebugBounds };
        }

        ctx.EmitChange(ctx.Context with { Tool = tool });
    }

    public static void OnSelectionEvent(SelectionEvent evt, StateManager ctx)
    {
        var selection = ctx.Context.Selection;
        if (evt.FixedInspector != FixedInspectorId.None)
        {
            ctx.EmitChange(ctx.Context with { Selection = selection with { FixedInspector = evt.FixedInspector } });
            return;
        }

        if (evt.Clear)
        {
            ctx.EmitChange(ctx.Context with
            {
                Selection = default,
                Tool = ctx.Context.Tool with { GizmoEnabled = false }
            });

            return;
        }

        if (evt.Asset == null && evt.SceneObject == null)
            throw new ArgumentException("Either Asset or SceneObject must be set");

        if (evt.Asset is { } asset)
        {
            if (selection.SelectedAssetId == asset) return;
            ctx.EmitChange(ctx.Context with { Selection = selection with { SelectedAssetId = asset } });
        }
        else if (evt.SceneObject is { } sceneObject)
        {
            if (selection.SelectedSceneId == sceneObject) return;
            
            ctx.EmitChange(ctx.Context with
            {
                Selection = selection with { SelectedSceneId = sceneObject },
                Tool = ctx.Context.Tool with { GizmoEnabled = sceneObject.IsValid() }
            });
        }
    }

    public static void OnSceneObjectEvent(SceneObjectEvent evt, StateManager ctx)
    {
        switch (evt.Action)
        {
            case EventAction.Rename:
                ArgumentException.ThrowIfNullOrWhiteSpace(evt.Name);
                var asset = EngineObjectStore.SceneController.GetSceneObject(evt.SceneObject);
                asset.SetName(evt.Name);
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public static void OnAssetUpdateEvent(AssetEvent evt, StateManager ctx)
    {
        switch (evt.Action)
        {
            case EventAction.Rename:
                ArgumentException.ThrowIfNullOrWhiteSpace(evt.Name);
                var asset = EngineObjectStore.AssetProvider.Get(evt.Asset);
                if (asset.Rename(evt.Name))
                    AssetListPanel.RenamedAsset = asset.Id;
                break;
            case EventAction.Reload:
                CommandDispatcher.InvokeEditorCommand(new AssetCommandRecord(CommandAssetAction.Reload, evt.Asset));
                break;
            default: throw new ArgumentOutOfRangeException();
        }
    }
}