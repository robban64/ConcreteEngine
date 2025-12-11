#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Core;

internal readonly record struct EditorModeState(
    EditorViewMode EditorMode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    public bool IsEmptyViewMode => EditorMode == EditorViewMode.None;
    public bool IsMetricState => EditorMode == EditorViewMode.Metrics;
    public bool IsEditorState => EditorMode == EditorViewMode.Editor;
    public bool IsEntityState => IsEditorState && LeftSidebar == LeftSidebarMode.Entities;

    public static EditorModeState MakeNone() => default;

    public static EditorModeState MakeMetrics() =>
        new(EditorViewMode.Metrics, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static EditorModeState MakeEditor() =>
        new(EditorViewMode.Editor, LeftSidebarMode.Default, RightSidebarMode.Default);
}