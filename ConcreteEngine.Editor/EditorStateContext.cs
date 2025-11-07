using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal static class EditorStateContext
{
    private const long FetchInterval = 1_500;

    public static EditorViewState ViewState { get; private set; }

    public static AssetStoreViewModel AssetViewModel { get; } = new();
    public static EntityListViewModel EntityListViewModel { get; } = new();
    public static CameraViewModel CameraModel { get; } = new();

    private static long _lastAction = TimeUtils.GetTimestamp();
    private static long _lastFetched = TimeUtils.GetTimestamp();


    internal static void Init()
    {
    }

    internal static EditorViewState PreRender()
    {
        GuiTheme.RightSidebarExpanded = ViewState.IsEditorState;
        MetricsApi.ToggleMetrics(ViewState.IsMetricState);

        if (ViewState.IsEditorState && ViewState.RightSidebar == RightSidebarMode.Camera)
        {
            if (TimeUtils.HasIntervalPassed(_lastFetched, FetchInterval))
            {
                RefreshCameraData();
                _lastFetched = TimeUtils.GetTimestamp();
            }
        }

        return ViewState;
    }

    public static void SetViewModeState(EditorViewMode mode)
    {
        if (mode == ViewState.EditorMode) SetState(EditorViewState.MakeNone());
        else if (mode == EditorViewMode.Editor) SetState(EditorViewState.MakeEditor());
        else if (mode == EditorViewMode.Metrics) SetState(EditorViewState.MakeMetrics());
    }

    public static void ToggleRightSidebarState(RightSidebarMode mode)
    {
        var newMode = mode == ViewState.RightSidebar ? RightSidebarMode.Default : mode;
        SetState(ViewState with { RightSidebar = newMode });
    }

    public static void SetLeftSidebarState(LeftSidebarMode mode)
    {
        if (mode == ViewState.LeftSidebar) return;
        SetState(ViewState with { LeftSidebar = mode });
    }

    private static void SetState(EditorViewState state)
    {
        var prevState = ViewState;
        ViewState = state;
        if (state.IsEditorState) OnEditorStateEnter(state, prevState);
    }

    private static void OnEditorStateEnter(EditorViewState state, EditorViewState prevState)
    {
        if (state.IsMetricState) throw new InvalidOperationException("Metric state is already in editor state");
        if (prevState.LeftSidebar == LeftSidebarMode.Assets && state.LeftSidebar != LeftSidebarMode.Assets)
            AssetViewModel.ResetState();

        switch (state.LeftSidebar)
        {
            case LeftSidebarMode.Assets:
                EditorApi.FillAssetStoreView?.Invoke(AssetViewModel.TypeSelection, AssetViewModel.AssetObjects);
                break;
            case LeftSidebarMode.Entities:
                if (EntityListViewModel.Entities.Count == 0) EditorApi.FillEntityView?.Invoke(EntityListViewModel);
                break;
        }

        switch (state.RightSidebar)
        {
            case RightSidebarMode.Default: break;
            case RightSidebarMode.Camera:
                RefreshCameraData();
                break;
            case RightSidebarMode.Light: break;
            case RightSidebarMode.Sky: break;
            case RightSidebarMode.Terrain: break;
        }
    }

    public static void RefreshCameraData()
    {
        if (!EditorApi.FetchCameraData(CameraModel.Generation, out var response))
            return;

        CameraModel.FromDataModel(in response);
        CameraPropertyComponent.UpdateStateFromViewModel();
    }

    private static bool CanExecute(int ms, bool print = false)
    {
        if (!TimeUtils.HasIntervalPassed(_lastAction, ms))
        {
            DevConsoleService.AddLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }

    public static void ExecuteReloadShader(AssetObjectViewModel viewModel)
    {
        if (!CanExecute(1000, true)) return;
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
            new EditorShaderPayload(viewModel.Name, EditorRequestAction.Reload));
    }

    public static void ExecuteSetEntityTransform(EntityViewModel entity)
    {
        //if (!CanExecute(25)) return;
        var payload = new EditorTransformPayload(entity.EntityId, in entity.Transform);
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.EntityTransform, in payload);
    }

    public static void ExecuteSetCameraTransform(in CameraEditorPayload payload)
    {
        //if (!CanExecute(25)) return;
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.CameraTransform, in payload);
    }
}