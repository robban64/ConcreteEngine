using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI.Assets;

namespace ConcreteEngine.Editor.Core;

internal static class EventHandler
{
    public static void OnModeEvent(ModeEvent evt, StateManager ctx)
    {
        ctx.EmitChange(ctx.Context with { Mode = new ModeContext { Id = evt.Mode } });
    }

    public static void OnToolEvent(ToolEvent evt, StateManager ctx)
    {
        if (evt.ShowDebugBounds is null && evt.GizmoEnabled is null)
            throw new ArgumentException();

        var tool = ctx.Context.Tool;

        if (evt.GizmoEnabled is { } gizmoEnabled)
        {
            tool = tool with { IsWorldGizmo = evt.IsWorldGizmo, GizmoOp = evt.GizmoOp, Enabled = gizmoEnabled };
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
        var tool = ctx.Context.Tool;
        if (evt.FixedInspector != FixedInspectorId.None)
        {
            ctx.EmitChange(ctx.Context with { Selection = selection with { FixedInspector = evt.FixedInspector } });
            return;
        }

        if (evt.Clear)
        {
            ctx.EmitChange(ctx.Context with { Selection = default, Tool = default });

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
            var isEmpty = sceneObject == SceneObjectId.Empty;

            ctx.EmitChange(ctx.Context with
            {
                Selection = selection with { SelectedSceneId = sceneObject },
                Tool = tool with
                {
                    Enabled = !isEmpty,
                    GizmoOp = tool.GizmoOp == TransformGizmoOp.None ? TransformGizmoOp.Translate : tool.GizmoOp,
                    IsWorldGizmo = true
                }
            });
        }
    }

    public static void OnSceneObjectEvent(SceneObjectEvent evt, StateManager ctx)
    {
        if (evt.Rename is { } name)
        {
            var asset = SceneStore.Instance.Get(evt.SceneObject);
            asset.SetName(name);
        }
    }

    public static void OnAssetUpdateEvent(AssetEvent evt, StateManager ctx)
    {
        if (evt.Rename is { } name)
        {
            var asset = AssetStore.Instance.Get(evt.Asset);
            if (asset.Rename(name))
                AssetListPanel.RenamedAsset = asset.Id;
        }
        else if (evt.Reload)
        {
            CommandDispatcher.DispatchCommand(new AssetCommandRecord(CommandAssetAction.Reload, evt.Asset, evt.Kind));
        }
    }
}