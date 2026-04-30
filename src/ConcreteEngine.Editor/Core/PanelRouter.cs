using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Assets;
using ConcreteEngine.Editor.UI.Core;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelRouter
{
    private readonly WindowManager _windows;

    internal PanelRouter(WindowManager windows, StateManager state)
    {
        _windows = windows;
        state.ContextChanged += OnContextChanged;
    }

    public void ForceResolve(StateManager state)
    {
        ResolveLeftWindow(state.Context, state.Context, true);
        ResolveRightWindow(state.Context, state.Context, true);
        ResolveToolbar(state.Context, state.Context, true);
    }

    private void OnContextChanged(EditorContext prev, EditorContext next)
    {
        ResolveLeftWindow(prev, next);
        ResolveRightWindow(prev, next);
        ResolveToolbar(prev, next);
    }

    private void ResolveLeftWindow(EditorContext prev, EditorContext next, bool force = false)
    {
        if (!force && prev.Mode == next.Mode) return;

        var target = next.Mode.Id switch
        {
            ModeId.Asset => typeof(AssetListPanel),
            ModeId.Scene => typeof(SceneListPanel),
            _ => typeof(AssetListPanel)
        };

        _windows.Navigate(WindowId.Left, target);
    }

    private void ResolveRightWindow(EditorContext prev, EditorContext next, bool force = false)
    {
        if (!force && prev.Selection == next.Selection && prev.Mode == next.Mode) return;

        var target = typeof(CameraPanel);
        if (prev.Selection.SelectedSceneId != next.Selection.SelectedSceneId)
            target = typeof(SceneInspectorPanel);
        else if (prev.Selection.SelectedAssetId != next.Selection.SelectedAssetId)
            target = typeof(AssetInspectorPanel);
        else if (prev.Selection.FixedInspector != next.Selection.FixedInspector)
        {
            target = next.Selection.FixedInspector switch
            {
                FixedInspectorId.Camera => typeof(CameraPanel),
                FixedInspectorId.Lighting => typeof(LightingPanel),
                FixedInspectorId.Visual => typeof(VisualPanel),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        _windows.Navigate(WindowId.Right, target);
    }

    private void ResolveToolbar(EditorContext prev, EditorContext next, bool force = false)
    {
        var mask = force ? ContextChangeMask.All : ContextChangeMask.None;
        if (prev.Mode != next.Mode) mask |= ContextChangeMask.Mode;
        if (prev.Tool != next.Tool) mask |= ContextChangeMask.Tool;
        if (prev.Selection != next.Selection) mask |= ContextChangeMask.Selection;

        for (var i = 0; i < 3; i++)
        {
            foreach (var item in _windows.GetToolbarGroup((ToolbarGroupAlignment)i))
            {
                if ((item.ChangeMask & mask) != 0)
                    item.OnStateChange(prev, next, item);
            }
        }

        _windows.SyncToolbar();
    }
}