using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelRouter
{
    private readonly WindowManager _windows;

    internal PanelRouter(StateManager state, WindowManager windows)
    {
        _windows = windows;
        state.ContextChanged += OnContextChanged;
    }

    public void ForceResolve(StateManager state)
    {
        ResolveToolbar(state.Context, state.Context, true);
    }

    private void OnContextChanged(EditorContext prev, EditorContext next)
    {
        ResolveToolbar(prev, next);
    }

    private static void ResolveToolbar(EditorContext prev, EditorContext next, bool force = false)
    {
        var mask = force ? ContextChangeMask.All : ContextChangeMask.None;
        if (prev.Tool != next.Tool) mask |= ContextChangeMask.Tool;
        if (prev.Selection != next.Selection) mask |= ContextChangeMask.Selection;

        for (var i = 0; i < TopMenuWindow.ToolbarGroupCount; i++)
        {
            foreach (var item in TopMenuWindow.GetToolbarGroup((ToolbarGroupAlignment)i))
            {
                if ((item.ChangeMask & mask) != 0)
                    item.OnStateChange(prev, next, item);
            }
        }

        TopMenuWindow.SyncToolbar();
    }
}