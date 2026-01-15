using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components.Layout;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int RefreshInterval = 4;

    private FrameStepper _refreshStepper = new(RefreshInterval);
    private PanelSize _panelSize;

    private readonly byte[] _buffer = new byte[512];

    private readonly StateManager _states;
    private readonly ComponentHub _stateHub;
    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly StateContext _stateContext;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly Topbar _topbar = new();
    private readonly LeftSidebar _leftSidebar = new();
    private readonly RightSidebar _rightSidebar = new();


    public EditorService()
    {
        _stateHub = new ComponentHub();
        _selectionManager = new SelectionManager();
        _states = new StateManager(_stateHub);
        _stateContext = new StateContext(_states, _stateHub, _selectionManager);

        _inputHandler = new InputHandler(_stateContext);
        _console = new ConsoleComponent();
    }

    public void OnResized() => _panelSize = _states.RefreshStyle(_console);

    public void Initialize()
    {
        _states.Initialize();
        _stateHub.Initialize(_stateContext);
        EditorInput.Initialize(_inputHandler);
    }


    public void Render(float delta)
    {
        PrepareFrame(delta);
        RefreshData();

        var currentMode = _states.ModeState;

        _topbar.Draw(_stateContext);

        DurationProfileTimer.Default.Begin();
        var ctx = new FrameContext(new SpanWriter(_buffer), delta, _states.ModeState);
        if (currentMode is { IsActive: true, IsCli: false })
        {
            ref readonly var panelSize = ref _panelSize;
            _leftSidebar.Draw(_stateHub.LeftSidebarState, _states, in panelSize, ref ctx);

            if (_stateHub.RightSidebarState is { } right)
                _rightSidebar.Draw(right, in panelSize, ref ctx);
        }
        DurationProfileTimer.Default.EndPrintSimple();

        _console.DrawConsole(_consoleService, ref ctx);
    }


    private void PrepareFrame(float delta)
    {
        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys(_states);
        }

        if (_states.CommitState()) _panelSize = _states.RefreshStyle(_console);
    }


    public void OnDiagnosticTick()
    {
        if (_states.ModeState.IsActive) _stateHub.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefreshData()
    {
        if (!_states.ModeState.IsActive || !_refreshStepper.Tick()) return;
        _stateHub.Update(_stateContext);
    }
}