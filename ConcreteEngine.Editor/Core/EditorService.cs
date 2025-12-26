using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using DataStore = ConcreteEngine.Editor.Store.EditorDataStore;

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private const int RefreshInterval = 4;

    private static EditorModeState ModeState => StateContext.ModeState;
    private static FrameStepper _refreshStepper = new(RefreshInterval);

    public static void Initialize()
    {
        ManagedStore.LoadResources();
        ModelManager.Initialize();
        StateContext.Initialize();
    }

    private static void PrepareFrame(float delta, bool blockInput)
    {
        var entity = DataStore.SelectedEntity;

        if (!ModeState.IsEntityState && entity.IsValid)
        {
            StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
        }
        else if (ModeState.RightSidebar == RightSidebarMode.Property && !entity.IsValid)
        {
            StateContext.SetRightSidebarState(RightSidebarMode.Camera);
        }
        
        if (!blockInput)
        {

            if (!EditorInput.IsMouseOverEditor())
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys();
        }
        var viewState = StateContext.ModeState;

        StateContext.CommitState();
        RefreshData();
        GuiTheme.PushTheme(viewState.IsEditorState);
    }
    
    internal static void Render(float delta, bool blockInput)
    {
        
        PrepareFrame(delta, blockInput);
        
        Topbar.Draw();

        var viewState = StateContext.ModeState;
        if (!viewState.IsEmptyViewMode)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 10f));

            LeftSidebar.Draw(GuiTheme.LeftSidebarWidth, offset: GuiTheme.TopbarHeight);
            if (viewState.RightSidebar != RightSidebarMode.Default || viewState.IsMetricState) 
                RightSidebar.Draw(delta, GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);

            ImGui.PopStyleVar(2);
        }

        ConsoleComponent.DrawConsole(GuiTheme.LeftSidebarWidth, GuiTheme.RightSidebarWidth);
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