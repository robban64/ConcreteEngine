#region

using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;

#endregion

namespace ConcreteEngine.Editor;

internal static class EditorService
{
    private const long RefreshInterval = 1_500;

    private static long _lastAction = TimeUtils.GetTimestamp();
    private static long _lastFetched = TimeUtils.GetTimestamp();

    private static long _lastRefresh = TimeUtils.GetTimestamp();

    private static EditorViewState ViewState => StateCtx.ViewState;


    static EditorService()
    {
        StateCtx.Init();
        CameraPropertyComponent.Init();
    }

    private static void RefreshData()
    {
        if (!ViewState.IsEditorState) return;
        if (!TimeUtils.HasIntervalPassed(_lastRefresh, RefreshInterval)) return;

        if (ViewState.RightSidebar == RightSidebarMode.Camera)
        {
            OnFetchUpdateCameraData();
            _lastRefresh = TimeUtils.GetTimestamp();
        }
    }

    internal static void Render()
    {
        GuiTheme.RightSidebarExpanded = ViewState.IsEditorState;
        MetricsApi.ToggleMetrics(ViewState.IsMetricState);

        RefreshData();

        Topbar.Draw();

        if (!StateCtx.ViewState.IsEmptyViewMode)
        {
            LeftSidebar.Draw(240, offset: GuiTheme.TopbarHeight);
            RightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);
        }

        DevConsoleService.Draw(240, GuiTheme.RightSidebarWidth);
    }

    public static bool OnFetchUpdateCameraData()
    {
        var gen = StateCtx.CameraModel.Generation;
        var result = OnApiFetch(gen, out var response, EditorApi.FetchCameraData);
        if (!result) return false;

        StateCtx.CameraModel.FromDataModel(in response);
        CameraPropertyComponent.UpdateStateFromViewModel();
        return true;
    }


    internal static bool OnFetchEntityData(int entityId, out EntityDataPayload response)
    {
        return OnApiFetch(entityId, out response, EditorApi.FetchEntityData);
    }

    private static bool OnApiFetch<TRequest, TResponse>(TRequest request, out TResponse? response,
        GenericDataRequest<TRequest, TResponse>? apiFetch)
    {
        if (!CanFetch(150) || apiFetch is null) return Fail(out response);
        apiFetch(request, out response);
        return true;
    }
    
    private static bool OnApiFill<TRequest, TResponse>(TRequest request, out TResponse? response,
        GenericDataRequest<TRequest, TResponse>? apiFetch)
    {
        if (!CanFetch(150) || apiFetch is null) return Fail(out response);
        apiFetch(request, out response);
        return true;
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
            DevConsoleService.AddLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }

    private static bool Fail<T>(out T? value)
    {
        value = default;
        return false;
    }
}