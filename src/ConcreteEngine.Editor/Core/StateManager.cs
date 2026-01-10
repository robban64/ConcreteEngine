using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;

namespace ConcreteEngine.Editor.Core;

internal static class StateManager
{
    public static ModeState ModeState { get; private set; }
    public static ModeState NextState { get; private set; }

    public static ModelStateContext? LeftSidebarState;
    public static ModelStateContext? RightSidebarState;

    private static long _lastAction = TimeUtils.GetTimestamp();

    internal static void Initialize()
    {
        ModeState = ModeState.MakeNone();
        NextState = ModeState.MakeCli();
    }

    internal static bool CommitState()
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
    public static void SetViewModeState(ViewMode mode, bool isMetrics)
    {
        NextState = mode switch
        {
            ViewMode.None => ModeState.MakeNone(),
            ViewMode.Cli => ModeState.MakeCli(),
            ViewMode.Main => isMetrics ? ModeState.MakeMetrics() : ModeState.MakeEditor(),
            _ => NextState
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ToggleRightSidebar(RightSidebarMode mode)
    {
        var newMode = mode == NextState.RightSidebar ? RightSidebarMode.Default : mode;
        NextState = NextState with { Mode = ViewMode.Main, RightSidebar = newMode };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetRightSidebarState(RightSidebarMode mode)
    {
        if (mode == NextState.RightSidebar) return;
        NextState = NextState with { Mode = ViewMode.Main, RightSidebar = mode };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetLeftSidebarState(LeftSidebarMode mode)
    {
        if (mode == NextState.LeftSidebar) return;
        NextState = NextState with { Mode = ViewMode.Main, LeftSidebar = mode };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ModelStateContext? GetLeftTransition(ModeState state)
    {
        return state.LeftSidebar switch
        {
            LeftSidebarMode.Metrics =>  ModelManager.MetricsStateContext,
            LeftSidebarMode.Assets => ModelManager.AssetStateContext,
            LeftSidebarMode.Scene => ModelManager.SceneStateContext,
            _ => null
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ModelStateContext? GetRightTransition(ModeState state)
    {

        return state.RightSidebar switch
        {
            RightSidebarMode.Property => ModelManager.SceneStateContext,
            RightSidebarMode.Metrics =>  ModelManager.MetricsStateContext,
            RightSidebarMode.Camera => ModelManager.CameraStateContext,
            RightSidebarMode.World => ModelManager.VisualStateContext,
            _ => null
        };
    }
    
    private static void Transition(ref ModelStateContext? current, ModelStateContext? next)
    {
        if (current is null && next is null)
            throw new ArgumentNullException(nameof(next), $"Both {nameof(current)} and to cannot be null");

        if (current is { Active: true }) current.InvokeAction(TransitionKey.Leave);
        
        current = next;
        
        if (next is { Active: false }) next.InvokeAction(TransitionKey.Enter);
    }

}