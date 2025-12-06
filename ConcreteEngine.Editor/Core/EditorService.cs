#region

using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Layout;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorService
{
    private const long RefreshInterval = 1_500;

    private static long _lastRefresh = TimeUtils.GetTimestamp();

    private static EditorModeState ModeState => StateContext.ModeState;


    internal static void Render(float delta, bool blockInput)
    {
        if (!blockInput)
        {
            if (!EditorInput.IsMouseOverEditor())
                EditorInput.UpdateMouse(delta);

            EditorInput.UpdateKeybinding();
        }

        StateContext.CommitState();
        GuiTheme.RightSidebarExpanded = ModeState.IsEditorState;
        MetricsApi.ToggleMetrics(ModeState.IsMetricState);

        RefreshData();

        GuiTheme.PushTheme();


        Topbar.Draw();
        if (!StateContext.ModeState.IsEmptyViewMode)
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
        if (!ModeState.IsEditorState) return;
        if (!TimeUtils.HasIntervalPassed(_lastRefresh, RefreshInterval)) return;

        if (ModeState.RightSidebar == RightSidebarMode.Camera && ModelManager.CameraStateContext.State is not null)
        {
            ModelManager.CameraStateContext.InvokeAction(TransitionKey.Refresh);
            _lastRefresh = TimeUtils.GetTimestamp();
        }

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