using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private const int RefreshInterval = 4;

    private static ModeState ModeState => StateManager.ModeState;
    private static FrameStepper _refreshStepper = new(RefreshInterval);

    public static void Initialize()
    {
        ModelManager.Initialize();
        StateManager.Initialize();
    }

    internal static void Render(float delta)
    {
        PrepareFrame(delta);

        Topbar.Draw();
        
        var currentMode = StateManager.ModeState;
        if (currentMode is { IsActive: true, IsCli: false })
        {
            if (StateManager.LeftSidebarState is { } left)
                LeftSidebar.Draw(left);

            if (StateManager.RightSidebarState is { } right)
                RightSidebar.Draw(right);
        }

        ConsoleComponent.DrawConsole(LeftSidebar.Width, RightSidebar.Width);
    }

    private static void PrepareFrame(float delta)
    {
        var selected = StoreHub.SelectedId;

        if (ModeState.LeftSidebar != LeftSidebarMode.Scene && selected.IsValid())
        {
            StateManager.SetLeftSidebarState(LeftSidebarMode.Scene);
            StateManager.SetRightSidebarState(RightSidebarMode.Property);
        }
        else if (ModeState.RightSidebar == RightSidebarMode.Property && !selected.IsValid())
        {
            StateManager.SetRightSidebarState(RightSidebarMode.Camera);
        }

        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys();
        }

        if (StateManager.CommitState()) RefreshStyle();

        RefreshData();
    }

    internal static void RefreshStyle()
    {
        if (ModeState.IsMetricsMode)
        {
            LeftSidebar.Width = GuiTheme.LeftSidebarCompactWidth;
            RightSidebar.Width = GuiTheme.RightSidebarCompactWidth;
        }
        else
        {
            LeftSidebar.Width = GuiTheme.LeftSidebarDefaultWidth;
            RightSidebar.Width = GuiTheme.RightSidebarDefaultWidth;
        }

        ConsoleComponent.CalculateSize(LeftSidebar.Width, RightSidebar.Width);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RefreshData()
    {
        if (!ModeState.IsActive || !_refreshStepper.Tick()) return;
        StateManager.LeftSidebarState?.Refresh();
        StateManager.RightSidebarState?.Refresh();

    }
}