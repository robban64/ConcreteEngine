#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Data;

internal readonly record struct EditorViewState(
    EditorViewMode EditorMode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    public bool IsEmptyViewMode => EditorMode == EditorViewMode.None;
    public bool IsMetricState => EditorMode == EditorViewMode.Metrics;
    public bool IsEditorState => EditorMode == EditorViewMode.Editor;

    public static EditorViewState MakeNone() => default;

    public static EditorViewState MakeMetrics() =>
        new(EditorViewMode.Metrics, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static EditorViewState MakeEditor() =>
        new(EditorViewMode.Editor, LeftSidebarMode.Default, RightSidebarMode.Default);
}