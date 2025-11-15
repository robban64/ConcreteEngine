#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ImGuiNET;


#endregion

namespace ConcreteEngine.Editor;

internal static class EditorService
{
    private const long RefreshInterval = 1_500;

    private static long _lastAction = TimeUtils.GetTimestamp();
    private static long _lastFetched = TimeUtils.GetTimestamp();

    private static long _lastRefresh = TimeUtils.GetTimestamp();

    private static EditorModeState ModeState => StateContext.ModeState;


    static EditorService()
    {
        ModelManager.SetupModelState();
        StateContext.Init();
    }


    internal static void Render(bool blockInput)
    {
        StateContext.CommitState();
        GuiTheme.RightSidebarExpanded = ModeState.IsEditorState;
        MetricsApi.ToggleMetrics(ModeState.IsMetricState);

        RefreshData();
        
        GuiTheme.PushTheme();

        if(!blockInput)
            CheckHotkeys();
        

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
    
    private static void CheckHotkeys()
    {
        if(ImGui.IsKeyPressed(ImGuiKey._1)) StateContext.ToggleLeftSidebar(LeftSidebarMode.Assets);
        else if(ImGui.IsKeyPressed(ImGuiKey._2)) StateContext.ToggleLeftSidebar(LeftSidebarMode.Entities);
        else if(ImGui.IsKeyPressed(ImGuiKey._3)) StateContext.ToggleRightSidebar(RightSidebarMode.Camera);
        else if(ImGui.IsKeyPressed(ImGuiKey._4)) StateContext.ToggleRightSidebar(RightSidebarMode.World);
        else if(ImGui.IsKeyPressed(ImGuiKey._5)) StateContext.ToggleRightSidebar(RightSidebarMode.Sky);
        else if(ImGui.IsKeyPressed(ImGuiKey._6)) StateContext.ToggleRightSidebar(RightSidebarMode.Terrain);
    }

    private static void RefreshData()
    {
        if (!ModeState.IsEditorState) return;
        if (!TimeUtils.HasIntervalPassed(_lastRefresh, RefreshInterval)) return;

        if (ModeState.RightSidebar == RightSidebarMode.Camera && ModelManager.CameraState.State is not null)
        {
            ModelManager.CameraState.InvokeAction(TransitionKey.Refresh);
            _lastRefresh = TimeUtils.GetTimestamp();
        }
        ModelManager.InvokeRefreshForModels();
    }


    internal static void OnFillAssetFiles(ModelState<AssetStoreViewModel> model, AssetObjectViewModel? asset)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));

        if (asset is null)
        {
            model.State.AssetFileObjects = [];
            return;
        }

        var body = new AssetRequestBody(asset.AssetId);
        var result = OnApiFill(body, EditorApi.FetchAssetObjectFiles) ?? [];
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

        var body = new AssetCategoryRequestBody(model.State.Category);
        var result = OnApiFill(body, EditorApi.FetchAssetStoreData) ?? [];
        model.State.AssetObjects = result;
    }



    internal static void OnFillEntities(ModelState<EntitiesViewModel> model)
    {
        ArgumentNullException.ThrowIfNull(model.State, nameof(model));
        var selected = model.State.Data.EntityId;
        var body = new EntityRequestBody(selected);
        var result = OnApiFill(body, EditorApi.FetchEntityView) ?? [];
        model.State.Entities = result;
    }

    private static TResponse? OnApiFill<TRequest, TResponse>(TRequest request,
        GenericRequest<TRequest, TResponse>? apiFetch) where TRequest : class where TResponse : class
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


}