#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Layout;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;
using DataStore = ConcreteEngine.Editor.Store.EditorDataStore;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private static EditorModeState ModeState => StateContext.ModeState;

    private static void PrepareFrame()
    {
        var newSelection = DataStore.State.SelectedEntity;
        DataStore.Input.EditorSelection.ClearFrame();
        DataStore.Input.EditorSelection.Id = newSelection;
        
        if (!ModeState.IsEntityState && newSelection.IsValid)
        {
            StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
        }
    }

    public static void ClearSelection()
    {
        ref var selection = ref DataStore.Input.EditorSelection;
        selection.Id = EditorId.Empty;
        selection.Action = EditorMouseAction.None;
        selection.IsRequesting = false;
        selection.IsDirty = true;
    }

    internal static void Render(float delta, bool blockInput)
    {
        PrepareFrame();
        var modeState = ModeState;

        if (!blockInput)
        {
            if (!EditorInput.IsMouseOverEditor())
                EditorInput.UpdateMouse(delta);

            EditorInput.UpdateKeybinding();
        }

        StateContext.CommitState();
        GuiTheme.RightSidebarExpanded = modeState.IsEditorState;
        MetricsApi.ToggleMetrics(modeState.IsMetricState);

        RefreshData();

        GuiTheme.PushTheme();


        Topbar.Draw();
        if (!modeState.IsEmptyViewMode)
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
        
        if(modeState.IsEntityState && DataStore.State.SelectedEntity.IsValid)
            ModelManager.EntitiesStateContext.EnqueueRefreshNextFrame();
        
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