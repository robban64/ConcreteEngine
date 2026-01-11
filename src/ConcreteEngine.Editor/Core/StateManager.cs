using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateManager(ModelStateHub stateHub)
{
    public ModeState ModeState { get; private set; }
    public ModeState NextState { get; private set; }

    public ModelStateComponent? LeftSidebarState;
    public ModelStateComponent? RightSidebarState;

    internal void Initialize()
    {
        ModeState = ModeState.MakeNone();
        NextState = ModeState.MakeCli();
    }

    internal bool CommitState()
    {
        if (ModeState == NextState) return false;
        var prev = ModeState;
        var next = ModeState = NextState;

        if (next.IsCli) return true;

        if (prev.LeftSidebar != next.LeftSidebar)
            Transition(ref LeftSidebarState, GetLeftTransition(next));

        if (prev.RightSidebar != next.RightSidebar)
            Transition(ref RightSidebarState, GetRightTransition(next));

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewModeState(ViewMode mode, bool isMetrics)
    {
        NextState = mode switch
        {
            ViewMode.None => ModeState.MakeNone(),
            ViewMode.Cli => ModeState.MakeCli(),
            ViewMode.Main => isMetrics ? ModeState.MakeMetrics() : ModeState.MakeEditor(),
            _ => NextState
        };
    }

    public void ToggleRightSidebar(RightSidebarMode mode)
    {
        var newMode = mode == NextState.RightSidebar ? RightSidebarMode.Default : mode;
        NextState = NextState with { Mode = ViewMode.Main, RightSidebar = newMode };
    }

    public void SetRightSidebarState(RightSidebarMode mode)
    {
        if (mode == NextState.RightSidebar) return;
        NextState = NextState with { Mode = ViewMode.Main, RightSidebar = mode };
    }

    public void SetLeftSidebarState(LeftSidebarMode mode)
    {
        if (mode == NextState.LeftSidebar) return;
        NextState = NextState with { Mode = ViewMode.Main, LeftSidebar = mode };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ModelStateComponent? GetLeftTransition(ModeState state)
    {
        return state.LeftSidebar switch
        {
            LeftSidebarMode.Metrics => stateHub.MetricsStateComponent,
            LeftSidebarMode.Assets => stateHub.AssetStateComponent,
            LeftSidebarMode.Scene => stateHub.SceneStateComponent,
            _ => null
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ModelStateComponent? GetRightTransition(ModeState state)
    {
        return state.RightSidebar switch
        {
            RightSidebarMode.Property => stateHub.SceneStateComponent,
            RightSidebarMode.Metrics => stateHub.MetricsStateComponent,
            RightSidebarMode.Camera => stateHub.CameraStateComponent,
            RightSidebarMode.World => stateHub.VisualStateComponent,
            _ => null
        };
    }

    private static void Transition(ref ModelStateComponent? current, ModelStateComponent? next)
    {
        if (current is null && next is null)
            throw new ArgumentNullException(nameof(next), $"Both {nameof(current)} and to cannot be null");

        if (current is { Active: true }) current.Leave();

        current = next;

        if (next is { Active: false }) next.Enter();
    }
}