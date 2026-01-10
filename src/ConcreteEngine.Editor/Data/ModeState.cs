using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal readonly record struct ModeState(
    ViewMode Mode,
    LeftSidebarMode LeftSidebar,
    RightSidebarMode RightSidebar)
{
    private bool HasMetricSidebar { get; } = LeftSidebar == LeftSidebarMode.Metrics && RightSidebar == RightSidebarMode.Metrics;
    public bool IsMetricsMode => Mode == ViewMode.Main && HasMetricSidebar;
    public bool IsEditorMode  => Mode == ViewMode.Main && !HasMetricSidebar;
    public bool IsActive => Mode != ViewMode.None;
    public bool IsCli => Mode == ViewMode.Cli;

    
    public static ModeState MakeNone() => default;
    public static ModeState MakeCli() =>
        new(ViewMode.Cli, LeftSidebarMode.Default, RightSidebarMode.Default);

    public static ModeState MakeMetrics() =>
        new(ViewMode.Main, LeftSidebarMode.Metrics, RightSidebarMode.Metrics);

    public static ModeState MakeEditor() =>
        new(ViewMode.Main, LeftSidebarMode.Assets, RightSidebarMode.Camera);
}