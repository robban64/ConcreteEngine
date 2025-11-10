#region

using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

internal static class EditorStateContext
{
    private static long _lastAction = TimeUtils.GetTimestamp();
    public static EditorViewState ViewState { get; private set; }
    
    private static IModelState? _leftSidebarState;
    private static IModelState? _rightSidebarState;

    internal static void Init()
    {
    }

    private static void TransitionLeftSidebar<T>(ModelState<T>? to) where T : class
    {
        if(_leftSidebarState is null && to is null)
            throw new ArgumentNullException(nameof(to), $"Both {nameof(_leftSidebarState)} and to cannot be null");

        
        _leftSidebarState?.InvokeAction(TransitionKey.Leave);
        _leftSidebarState = to;
        to?.InvokeAction(TransitionKey.Enter);
    }
    
    private static void TransitionRightSidebar<T>(ModelState<T>? to) where T : class
    {
        if(_rightSidebarState is null && to is null)
            throw new ArgumentNullException(nameof(to), $"Both {nameof(_rightSidebarState)} and to cannot be null");

        _rightSidebarState?.InvokeAction(TransitionKey.Leave);
        _rightSidebarState = to;
        to?.InvokeAction(TransitionKey.Enter);
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
                TransitionLeftSidebar();
                EditorService.OnFillAssetStore(EditorAssetCategory.None);
                break;
            case LeftSidebarMode.Entities:
                if (EntitiesViewModel.Entities.Count == 0) 
                    EditorService.OnFillEntities();
                break;
        }

        switch (state.RightSidebar)
        {
            case RightSidebarMode.Default: break;
            case RightSidebarMode.Camera:
                EditorService.OnFetchCameraData();
                break;
            case RightSidebarMode.Light: break;
            case RightSidebarMode.Sky: break;
            case RightSidebarMode.Terrain: break;
        }
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

    public static void ExecuteSetEntityTransform(in EntityTransformPayload payload)
    {
        //if (!CanExecute(25)) return;
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.EntityTransform, in payload);
    }

    public static void ExecuteSetCameraTransform(in CameraEditorPayload payload)
    {
        //if (!CanExecute(25)) return;
        CommandDispatcher.InvokeEditorCommand(CoreCmdNames.CameraTransform, in payload);
    }
}