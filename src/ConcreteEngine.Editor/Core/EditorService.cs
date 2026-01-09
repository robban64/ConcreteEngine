using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using DataStore = ConcreteEngine.Editor.Core.EditorDataStore;

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private const int RefreshInterval = 4;

    private static ModeState ModeState => StateContext.ModeState;
    private static FrameStepper _refreshStepper = new(RefreshInterval);
    
    public static void Initialize()
    {
        ModelManager.Initialize();
        StateContext.Initialize();
    }

    private static void PrepareFrame(float delta)
    {
        var entity = DataStore.SelectedSceneObj;

        if (!ModeState.IsSceneState && entity.IsValid())
        {
            StateContext.SetLeftSidebarState(LeftSidebarMode.Scene);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
        }
        else if (ModeState.RightSidebar == RightSidebarMode.Property && !entity.IsValid())
        {
            StateContext.SetRightSidebarState(RightSidebarMode.Camera);
        }

        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys();
        }
        
        if (StateContext.CommitState()) RefreshStyle();

        RefreshData();
    }

    internal static void RefreshStyle()
    {
        if (ModeState.IsMetricState)
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

    internal static void Render(float delta)
    {
        PrepareFrame(delta);

        Topbar.Draw();

        var viewState = StateContext.ModeState;
        if (!viewState.IsEmptyViewMode)
        {

            LeftSidebar.Draw();
            if (viewState.RightSidebar != RightSidebarMode.Default || viewState.IsMetricState)
                RightSidebar.Draw(delta);

        }

        ConsoleComponent.DrawConsole(LeftSidebar.Width, RightSidebar.Width);

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RefreshData()
    {
        var modeState = ModeState;

        if (!modeState.IsEditorState || !_refreshStepper.Tick()) return;

        switch (modeState.RightSidebar)
        {
            case RightSidebarMode.Camera: ModelManager.CameraStateContext.EnqueueRefreshNextFrame(); break;
            case RightSidebarMode.World: ModelManager.WorldRenderStateContext.EnqueueRefreshNextFrame(); break;
        }

        ModelManager.InvokeRefreshForModels();
    }
}