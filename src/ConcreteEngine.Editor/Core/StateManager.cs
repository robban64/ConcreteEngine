using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateManager(ComponentHub stateHub)
{
    public ModeState ModeState { get; private set; } = ModeState.MakeNone();
    public ModeState NextState { get; private set; } = ModeState.MakeCli();
    

    public void Initialize()
    {
        ModeState = ModeState.MakeNone();
        NextState = ModeState.MakeCli();
    }

    public bool CommitState()
    {
        if (ModeState == NextState) return false;
        var prev = ModeState;
        var next = ModeState = NextState;

        if (next.IsCli) return true;

        if (prev.LeftSidebar != next.LeftSidebar)
        {
            var currentState = stateHub.LeftSidebarState;
            var nextState = stateHub.GetLeftTransition(next.LeftSidebar);
            if (currentState?.Active == true) currentState.Leave();
            if (nextState?.Active == false) nextState.Enter();
            stateHub.LeftSidebarState = nextState;
        }

        if (prev.RightSidebar != next.RightSidebar)
        {
            var currentState = stateHub.RightSidebarState;
            var nextState = stateHub.GetRightTransition(next.RightSidebar);
            if (currentState?.Active == true) currentState.Leave();
            if (nextState?.Active == false) nextState.Enter();
            stateHub. RightSidebarState = nextState;
        }

        return true;
    }
    
    public PanelSize RefreshStyle(ConsoleComponent console)
    {
        var vp = ImGui.GetMainViewport();

        var isEditor = ModeState.IsEditorMode;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        console.CalculateSize(left, right);
        
        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = stateHub.LeftSidebarState != null;
        var leftHeight = hasLeftSidebar ? height : 52;

        return new PanelSize
        {
            LeftSize = new Vector2(left, leftHeight),
            LeftPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight },
            RightSize = new Vector2(right, height),
            RightPosition = new Vector2(vp.WorkPos.X + vp.WorkSize.X - right, vp.WorkPos.Y + GuiTheme.TopbarHeight)
        };
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

}