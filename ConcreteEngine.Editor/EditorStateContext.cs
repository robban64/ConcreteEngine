#region

using ConcreteEngine.Common;
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
    public static EditorModeState ModeState { get; private set; }
    public static EditorModeState NextState { get; private set; }

    private static IModelState? _leftSidebarState;
    private static IModelState? _rightSidebarState;

    private static long _lastAction = TimeUtils.GetTimestamp();

    internal static void Init()
    {
    }
    
    
    internal static void CommitState()
    {
        if(ModeState == NextState) return;
        var prev = ModeState;
        ModeState = NextState;
        if (ModeState.IsEditorState)
            OnEditorStateEnter(ModeState, prev);
        else if (prev.IsEditorState)
            OnEditorStateLeave(prev, ModeState);
    }


    private static void TransitionLeftSidebar<T>(ModelState<T>? to) where T : class
    {
        if (_leftSidebarState is null && to is null)
            throw new ArgumentNullException(nameof(to), $"Both {nameof(_leftSidebarState)} and to cannot be null");

        _leftSidebarState?.InvokeAction(TransitionKey.Leave);
        _leftSidebarState = to;
        to?.InvokeAction(TransitionKey.Enter);
    }

    private static void TransitionRightSidebar<T>(ModelState<T>? to) where T : class
    {
        if (_rightSidebarState is null && to is null)
            throw new ArgumentNullException(nameof(to), $"Both {nameof(_rightSidebarState)} and to cannot be null");

        _rightSidebarState?.InvokeAction(TransitionKey.Leave);
        _rightSidebarState = to;
        to?.InvokeAction(TransitionKey.Enter);
    }

    public static void SetViewModeState(EditorViewMode mode)
    {
        if (mode == NextState.EditorMode) NextState =(EditorModeState.MakeNone());
        else if (mode == EditorViewMode.Editor) NextState =(EditorModeState.MakeEditor());
        else if (mode == EditorViewMode.Metrics) NextState =(EditorModeState.MakeMetrics());
    }

    public static void ToggleRightSidebarState(RightSidebarMode mode)
    {
        var newMode = mode == NextState.RightSidebar ? RightSidebarMode.Default : mode;
        NextState =(NextState with { RightSidebar = newMode });
    }

    public static void SetLeftSidebarState(LeftSidebarMode mode)
    {
        if (mode == NextState.LeftSidebar) return;
        NextState = NextState with { LeftSidebar = mode };
    }

    private static void OnEditorStateEnter(EditorModeState state, EditorModeState prev)
    {
        if (prev.LeftSidebar != state.LeftSidebar)
            TransitionLeft(state);

        // Right sidebar
        if (prev.RightSidebar != state.RightSidebar)
            TransitionRight(state);

        return;

        static void TransitionLeft(EditorModeState state)
        {
            switch (state.LeftSidebar)
            {
                case LeftSidebarMode.Assets:
                    TransitionLeftSidebar(ModelManager.AssetState);
                    break;
                case LeftSidebarMode.Entities:
                    TransitionLeftSidebar(ModelManager.EntitiesState);
                    break;
            }

        }
        static void TransitionRight(EditorModeState state)
        {
            switch (state.RightSidebar)
            {
                case RightSidebarMode.Default: break;
                case RightSidebarMode.Camera:
                    TransitionRightSidebar(ModelManager.CameraState);
                    break;
                case RightSidebarMode.Light: break;
                case RightSidebarMode.Sky: break;
                case RightSidebarMode.Terrain: break;
            }
        }
    }
    
    private static void OnEditorStateLeave(EditorModeState state, EditorModeState nextState)
    {
        _leftSidebarState?.InvokeAction(TransitionKey.Leave);
        _rightSidebarState?.InvokeAction(TransitionKey.Leave);

        _leftSidebarState = null;
        _rightSidebarState = null;
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

}