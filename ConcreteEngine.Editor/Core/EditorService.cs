#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components;
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
    private static EditorModeState ModeState => StateContext.ModeState;

    public static void Initialize()
    {
        EditorManagedStore.Initialize();
        ModelManager.Initialize();
        StateContext.Initialize();
    }

    private static void PrepareFrame()
    {
        var newSelection = DataStore.State.SelectedEntity;
        
        if (!ModeState.IsEntityState && newSelection.IsValid)
        {
            StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
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

        if (!modeState.IsEditorState) return;
        
        /*
        if(modeState.IsEntityState && DataStore.State.SelectedEntity.IsValid)
            ModelManager.EntitiesStateContext.EnqueueRefreshNextFrame();
            */
        
        ModelManager.InvokeRefreshForModels();
    }

/*
     private static long _lastAction = TimeUtils.GetTimestamp();
     private static long _lastFetched = TimeUtils.GetTimestamp();

     private static TResponse? OnApiFill<TRequest, TResponse>(TRequest request,
       ApiModelRequestDel<TRequest, TResponse>? apiFetch) where TRequest : class where TResponse : class
   {
       if (!CanFetch(150) || apiFetch is null) return null;
       Console.WriteLine($"Api Fill: {typeof(TResponse).Name}");
       return apiFetch(request);
   }

    private static bool CanFetch(int ms)
    {
        if (!TimeUtils.HasIntervalPassed(_lastFetched, ms)) return false;
        _lastFetched = TimeUtils.GetTimestamp();
        return true;
    }

    private static bool CanExecute(int ms)
    {
        if (!TimeUtils.HasIntervalPassed(_lastAction, ms))
        {
            ConsoleService.SendLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }
    */
}