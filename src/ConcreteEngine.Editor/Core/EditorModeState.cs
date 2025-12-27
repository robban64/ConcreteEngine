using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal readonly record struct EditorModeState(
    ViewMode Mode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    public bool IsEmptyViewMode => Mode == ViewMode.None;
    public bool IsMetricState => Mode == ViewMode.Metrics;
    public bool IsEditorState => Mode == ViewMode.Editor;
    public bool IsEntityState => IsEditorState && LeftSidebar == LeftSidebarMode.Entities;

    public static EditorModeState MakeNone() => default;

    public static EditorModeState MakeMetrics() =>
        new(ViewMode.Metrics, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static EditorModeState MakeEditor() =>
        new(ViewMode.Editor, LeftSidebarMode.Default, RightSidebarMode.Default);
}