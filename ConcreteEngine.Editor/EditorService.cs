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

    private static EditorModeState ModeState => StateCtx.ModeState;


    static EditorService()
    {
        EditorStateManager.SetupModelState();
        StateCtx.Init();
    }

    private static void RefreshData()
    {
        if (!ModeState.IsEditorState) return;
        if (!TimeUtils.HasIntervalPassed(_lastRefresh, RefreshInterval)) return;

        if (ModeState.RightSidebar == RightSidebarMode.Camera && EditorStateManager.CameraModelState.State is not null)
        {
            OnFetchCameraData(EditorStateManager.CameraModelState);
            _lastRefresh = TimeUtils.GetTimestamp();
        }
    }

    internal static void Render()
    {
        StateCtx.CommitState();
        

        GuiTheme.RightSidebarExpanded = ModeState.IsEditorState;
        MetricsApi.ToggleMetrics(ModeState.IsMetricState);

        RefreshData();

        Topbar.Draw();

        if (!StateCtx.ModeState.IsEmptyViewMode)
        {
            LeftSidebar.Draw(240, offset: GuiTheme.TopbarHeight);
            RightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);
        }

        DevConsoleService.Draw(240, GuiTheme.RightSidebarWidth);
    }
    

    internal static void OnFillAssetFiles(ModelState<AssetStoreViewModel> model, AssetObjectViewModel? asset)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));
        
        if (asset is null)
        {
            model.State.AssetFileObjects = [];
            return;
        }

        var result = OnApiFill(asset.AssetId, EditorApi.FillAssetObjectFiles) ?? [];
        model.State.AssetFileObjects = result;
    }

    internal static void OnFillAssetStore(ModelState<AssetStoreViewModel> model, EditorAssetCategory? category = null)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));

        if (category is { } assetCategory)
        {
            if (assetCategory == model.State.Category) return;
            model.State.Category = assetCategory;
        }
        
        var result = OnApiFill(model.State.Category, EditorApi.FillAssetStoreView) ?? [];
        model.State.AssetObjects = result;

/*
        if (model.State.Category == EditorAssetCategory.None)
        {
            model.State.ResetState(true);
            return;
        }
*/
    }


    internal static bool OnFetchCameraData(ModelState<CameraViewModel> model)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));
        var gen = model.State.Generation;
        var result = OnApiFetch(gen, out var response, EditorApi.FetchCameraData);
        if (!result) return false;

        if(gen == 0) model.State.InitData(in response);
        else model.State.UpdateData(in response);
        
        return true;
    }

    internal static void OnFillEntities(ModelState<EntitiesViewModel> model)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));
        var selected = model.State.SelectedEntityId;
        var result = OnApiFill(selected, EditorApi.FillEntityView) ?? [];
        model.State.Entities = result;
    }
    
    internal static bool OnFetchEntityData(ModelState<EntitiesViewModel> model, EntityRecord entity)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));
        if (!OnApiFetch(entity.EntityId, out var response, EditorApi.FetchEntityData)) return false;
        
        InvalidOpThrower.ThrowIf(entity.EntityId != response.EntityId);
        model.State.FromData(in response);
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