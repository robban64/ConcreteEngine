using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal readonly record struct ModeState(
    ViewMode Mode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    public bool IsEmptyViewMode => Mode == ViewMode.None;
    public bool IsMetricState => Mode == ViewMode.Metrics;
    public bool IsEditorState => Mode == ViewMode.Editor;
    public bool IsSceneState => IsEditorState && LeftSidebar == LeftSidebarMode.Scene;

    public static ModeState MakeNone() => default;

    public static ModeState MakeMetrics() =>
        new(ViewMode.Metrics, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static ModeState MakeEditor() =>
        new(ViewMode.Editor, LeftSidebarMode.Default, RightSidebarMode.Default);
}