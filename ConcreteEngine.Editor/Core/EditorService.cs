#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using DataStore = ConcreteEngine.Editor.Store.EditorDataStore;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private const int RefreshInterval = 4;

    private static EditorModeState ModeState => StateContext.ModeState;
    private static SimpleFrameTicker _refreshTicker = new(RefreshInterval);

    public static void Initialize()
    {
        EditorManagedStore.Initialize();
        ModelManager.Initialize();
        StateContext.Initialize();
    }

    private static void PrepareFrame()
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
    }

    private static void ProcessInput(float delta)
    {
        if (!EditorInput.IsMouseOverEditor())
            EditorInput.UpdateMouse(delta);

        EditorInput.UpdateKeybinding();
    }


    internal static void Render(float delta, bool blockInput)
    {
        PrepareFrame();

        if (!blockInput) ProcessInput(delta);

        StateContext.CommitState();
        GuiTheme.RightSidebarExpanded = ModeState.IsEditorState;
        MetricsApi.ToggleMetrics(ModeState.IsMetricState);

        RefreshData();

        Draw();
    }

    private static void Draw()
    {
        GuiTheme.PushTheme();
        Topbar.Draw();
        if (!ModeState.IsEmptyViewMode)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 10f));

            LeftSidebar.Draw(GuiTheme.LeftSidebarWidth, offset: GuiTheme.TopbarHeight);
            RightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);

            ImGui.PopStyleVar(2);
        }

        ConsoleService.Draw(GuiTheme.LeftSidebarWidth, GuiTheme.RightSidebarWidth);
    }

    private static void RefreshData()
    {
        var modeState = ModeState;

        if (!_refreshTicker.Tick()) return;
        if (!modeState.IsEditorState) return;

        switch (modeState.RightSidebar)
        {
            case RightSidebarMode.Camera: ModelManager.CameraStateContext.EnqueueRefreshNextFrame(); break;
            case RightSidebarMode.World: ModelManager.WorldRenderStateContext.EnqueueRefreshNextFrame(); break;
        }

        ModelManager.InvokeRefreshForModels();

        /*
        if(modeState.IsEntityState && DataStore.State.SelectedEntity.IsValid)
            ModelManager.EntitiesStateContext.EnqueueRefreshNextFrame();
            */
    }
}