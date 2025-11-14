#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.ViewModel;

internal readonly record struct EditorModeState(
    EditorViewMode EditorMode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    public bool IsEmptyViewMode => EditorMode == EditorViewMode.None;
    public bool IsMetricState => EditorMode == EditorViewMode.Metrics;
    public bool IsEditorState => EditorMode == EditorViewMode.Editor;

    public static EditorModeState MakeNone() => default;

    public static EditorModeState MakeMetrics() =>
        new(EditorViewMode.Metrics, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static EditorModeState MakeEditor() =>
        new(EditorViewMode.Editor, LeftSidebarMode.Entities, RightSidebarMode.Default);
}