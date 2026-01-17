using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int RefreshInterval = 4;

    private FrameStepper _refreshStepper = new(RefreshInterval);

    private readonly byte[] _buffer = new byte[512];

    private readonly StateManager _states;
    private readonly ComponentHub _stateHub;
    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly StateContext _stateContext;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly Layout _layout;


    public EditorService()
    {
        _stateHub = new ComponentHub();
        _selectionManager = new SelectionManager();
        _states = new StateManager(_stateHub);
        _stateContext = new StateContext(_states, _stateHub, _selectionManager);

        _inputHandler = new InputHandler(_stateContext);
        _console = new ConsoleComponent();
        _consoleService.Console = _console;


        _layout = new Layout(_stateContext);
    }

    public void UpdateStyle() 
        => _layout.SetPanelSize(_states.RefreshStyle(_console));

    public void Initialize()
    {
        _states.Initialize();
        _stateHub.Initialize(_stateContext);
        EditorInput.Initialize(_inputHandler);
    }


    public void Render(float delta)
    {
        var currentMode = _states.ModeState;
        PrepareFrame(delta);
        if (currentMode.Mode == ViewMode.None) return;

        var ctx = new FrameContext(new SpanWriter(_buffer), delta, _states.ModeState);
        RefreshData();
        _layout.DrawTop();
        _layout.DrawLeft(_stateHub.LeftSidebarState, ctx);
        _layout.DrawRight(_stateHub.RightSidebarState, ctx);
        _console.DrawConsole(_consoleService, ctx);
    }


    private void PrepareFrame(float delta)
    {
        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys(_states);
        }

        if (_states.CommitState()) UpdateStyle();
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