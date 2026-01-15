using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Layout;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int RefreshInterval = 4;

    private FrameStepper _refreshStepper = new(RefreshInterval);
    private PanelSize _panelSize;

    private readonly StateManager _states;
    private readonly ComponentHub _stateHub;
    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly GlobalContext _globalContext;

    private readonly Topbar _topbar = new();
    private readonly LeftSidebar _leftSidebar = new();
    private readonly RightSidebar _rightSidebar = new();

    public EditorService()
    {
        _stateHub = new ComponentHub();
        _selectionManager = new SelectionManager();
        _states = new StateManager(_stateHub);
        _globalContext = new GlobalContext(_states, _stateHub, _selectionManager);

        _inputHandler = new InputHandler(_globalContext);
    }

    public void OnResized() => _panelSize = _states.RefreshStyle();

    public void Initialize()
    {
        _states.Initialize();
        _stateHub.Initialize(_globalContext);
        EditorInput.Initialize(_inputHandler);
    }

    public void Render(float delta)
    {
        PrepareFrame(delta);
        RefreshData();

        var currentMode = _states.ModeState;
        
        _topbar.Draw(_globalContext);

        DurationProfileTimer.Default.Begin();
        if (currentMode is { IsActive: true, IsCli: false })
        {
            Span<byte> buffer = stackalloc byte[128];
            var ctx = new FrameContext(buffer, delta, currentMode);

            _leftSidebar.Draw(_stateHub.LeftSidebarState, _states, ctx, in _panelSize);
            if (_stateHub.RightSidebarState is { } right) _rightSidebar.Draw(right, ctx, in _panelSize);
        }
        DurationProfileTimer.Default.EndPrintSimple();

        ConsoleComponent.DrawConsole();
    }


    private void PrepareFrame(float delta)
    {
        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys(_states);
        }

        if (_states.CommitState()) _panelSize = _states.RefreshStyle();

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
        _stateHub.Update(_globalContext);
    }
}