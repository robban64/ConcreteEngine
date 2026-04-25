using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Assets;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelRouter
{
    private readonly WindowManager _windows;

    internal PanelRouter(WindowManager windows, StateManager state)
    {
        _windows = windows;
        state.ContextChanged = OnContextChanged;
    }

    public void ForceResolve(StateManager state)
    {
        ResolveRightWindow(state.Context, state.Context, true);
        ResolveToolbar(state.Context, state.Context);
    }

    private void OnContextChanged(EditorContext prev, EditorContext next)
    {
        ResolveRightWindow(prev, next);
        ResolveToolbar(prev, next);
    }


    private void ResolveRightWindow(EditorContext prev, EditorContext next, bool force = false)
    {
        if (!force && prev.Selection == next.Selection && prev.Mode == next.Mode) return;

        Type target = typeof(CameraPanel);
        if (!prev.Selection.HasSceneObject && next.Selection.HasSceneObject)
            target = typeof(SceneInspectorPanel);
        else if (!prev.Selection.HasAsset && next.Selection.HasAsset)
            target = typeof(AssetInspectorPanel);
        else if (prev.Selection.FixedInspector != next.Selection.FixedInspector)
        {
            target = next.Selection.FixedInspector switch
            {
                FixedInspectorId.Camera => typeof(CameraPanel),
                FixedInspectorId.Atmosphere => typeof(AtmospherePanel),
                FixedInspectorId.Lighting => typeof(LightingPanel),
                FixedInspectorId.Visual => typeof(VisualPanel),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        _windows.Navigate(WindowId.Right, target);
    }

    private void ResolveToolbar(EditorContext prev, EditorContext next)
    {
        for (var i = 0; i < 3; i++)
        {
            foreach (var item in _windows.GetToolbarGroup((ToolbarGroupAlignment)i))
                item.OnStateChange(prev, next, item);
        }

        _windows.SyncToolbar();
    }
}