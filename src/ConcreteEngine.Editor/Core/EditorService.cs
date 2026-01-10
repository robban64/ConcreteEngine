using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal sealed class EditorService
{
    private const int RefreshInterval = 4;

    private FrameStepper _refreshStepper = new(RefreshInterval);

    private readonly StateManager _states;
    private readonly ModelStateHub _stateHub;

    private readonly GlobalContext _globalContext;

    public EditorService()
    {
        _stateHub = new ModelStateHub();
        _states = new StateManager(_stateHub);
        _globalContext  = new GlobalContext(_states);
    }

    public void Initialize()
    {
        _states.Initialize();
        _stateHub.Initialize(_globalContext);
    }

    public void Render(float delta)
    {
        DurationProfileTimer.Default2.Begin();
        PrepareFrame(delta);

        var states = _states;
        var currentMode = states.ModeState;

        Topbar.Draw(states);

        if (currentMode is { IsActive: true, IsCli: false })
        {
            if (_states.LeftSidebarState is { } left)
                LeftSidebar.Draw(left, states);

            if (_states.RightSidebarState is { } right)
                RightSidebar.Draw(right, states);
        }

        DurationProfileTimer.Default2.EndPrint();

        ConsoleComponent.DrawConsole(LeftSidebar.Width, RightSidebar.Width);
    }

    public void OnDiagnosticTick()
    {
        ConsoleGateway.OnTick();

        if (_states.ModeState.IsMetricsMode)
            MetricsApi.Tick();
    }

    private void PrepareFrame(float delta)
    {
        var selected = StoreHub.SelectedId;
        var mode = _states.ModeState;

        if (mode.LeftSidebar != LeftSidebarMode.Scene && selected.IsValid())
        {
            _states.SetLeftSidebarState(LeftSidebarMode.Scene);
            _states.SetRightSidebarState(RightSidebarMode.Property);
        }
        else if (mode.RightSidebar == RightSidebarMode.Property && !selected.IsValid())
        {
            _states.SetRightSidebarState(RightSidebarMode.Camera);
        }

        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta, _stateHub);

            EditorInput.CheckHotkeys(_states);
        }

        if (_states.CommitState()) RefreshStyle();

        RefreshData();
    }

    internal void RefreshStyle()
    {
        if (_states.ModeState.IsMetricsMode)
        {
            LeftSidebar.Width = GuiTheme.LeftSidebarCompactWidth;
            RightSidebar.Width = GuiTheme.RightSidebarCompactWidth;
        }
        else
        {
            LeftSidebar.Width = GuiTheme.LeftSidebarDefaultWidth;
            RightSidebar.Width = GuiTheme.RightSidebarDefaultWidth;
        }

        ConsoleComponent.CalculateSize(LeftSidebar.Width, RightSidebar.Width);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefreshData()
    {
        if (!_states.ModeState.IsActive || !_refreshStepper.Tick()) return;
        _states.LeftSidebarState?.Refresh();
        _states.RightSidebarState?.Refresh();
    }
}