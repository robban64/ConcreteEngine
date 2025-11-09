#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;

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
            OnFetchCameraData();
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


    internal static void OnFillEntities()
    {
        var selected = StateCtx.EntityListViewModel.SelectedEntityId;
        var result = OnApiFill(selected, EditorApi.FillEntityView) ?? [];
        StateCtx.EntityListViewModel.Entities = result;
    }

    internal static void OnFillAssetFiles(AssetObjectViewModel? asset)
    {
        if (asset is null)
        {
            StateCtx.AssetViewModel.AssetFileObjects = [];
            return;
        }

        var result = OnApiFill(asset.AssetId, EditorApi.FillAssetObjectFiles) ?? [];
        StateCtx.AssetViewModel.AssetFileObjects = result;
    }

    internal static void OnFillAssetStore(EditorAssetSelection? selection = null)
    {
        if (selection is { } assetSelection)
        {
            if (assetSelection == StateCtx.AssetViewModel.Selection) return;
            StateCtx.AssetViewModel.Selection = assetSelection;
        }

        if (StateCtx.AssetViewModel.Selection == EditorAssetSelection.None)
        {
            StateCtx.AssetViewModel.ResetState(true);
            return;
        }

        var result = OnApiFill(StateCtx.AssetViewModel.Selection, EditorApi.FillAssetStoreView) ?? [];
        StateCtx.AssetViewModel.AssetObjects = result;
    }


    internal static bool OnFetchCameraData()
    {
        var gen = StateCtx.CameraModel.Generation;
        var result = OnApiFetch(gen, out var response, EditorApi.FetchCameraData);
        if (!result) return false;

        StateCtx.CameraModel.FromDataModel(in response);
        return true;
    }

    internal static bool OnFetchEntityData(EntityViewModel entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));
        if (!OnApiFetch(entity.EntityId, out var response, EditorApi.FetchEntityData)) return false;
        InvalidOpThrower.ThrowIf(entity.EntityId != response.EntityId);
        EntitiesComponent.SetEntityDataState(new EntityDataState(in response));
        return true;
    }
    
    

    private static bool OnApiFetch<TRequest, TResponse>(TRequest request, out TResponse? response,
        GenericDataRequest<TRequest, TResponse>? apiFetch)
    {
        if (!CanFetch(150) || apiFetch is null) return FailOut(out response);
        Console.WriteLine($"Api Fetch: {typeof(TResponse).Name}");
        apiFetch(request, out response);
        return true;
    }

    private static TResponse? OnApiFill<TRequest, TResponse>(TRequest request,
        GenericRequest<TRequest, TResponse>? apiFetch)
    {
        if (!CanFetch(150) || apiFetch is null) return default;
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
            DevConsoleService.AddLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }

    private static bool FailOut<T>(out T? value)
    {
        value = default;
        return false;
    }
}