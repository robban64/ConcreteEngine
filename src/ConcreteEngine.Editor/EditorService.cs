using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Layout;
using ConcreteEngine.Editor.Metrics;
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
        _globalContext = new GlobalContext(_states, _stateHub,_selectionManager);
        
        _inputHandler = new InputHandler(_globalContext);
    }

    public void Initialize()
    {
        _states.Initialize();
        _stateHub.Initialize(_globalContext);
        EditorInput.Initialize(_inputHandler);
    }

    public void Render(float delta)
    {
        DurationProfileTimer.Default.Begin();
        PrepareFrame(delta);

        var states = _states;
        var currentMode = states.ModeState;

        Span<byte> buffer = stackalloc byte[128];
        var ctx = new FrameContext(buffer, delta, currentMode);

        _topbar.Draw(_globalContext);

        if (currentMode is { IsActive: true, IsCli: false })
        {
            _leftSidebar.Draw(states.LeftSidebarState, states, ctx, in _panelSize);

            if (states.RightSidebarState is { } right)
                _rightSidebar.Draw(right, ctx, in _panelSize);
        }

        DurationProfileTimer.Default.EndPrintSimple();

        ConsoleComponent.DrawConsole();
    }


    private void PrepareFrame(float delta)
    {
        var selected = _selectionManager.SelectedId;
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
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys(_states);
        }

        if (_states.CommitState()) RefreshStyle();

        RefreshData();
    }

    internal void RefreshStyle()
    {
        var vp = ImGui.GetMainViewport();

        var isEditor = _states.ModeState.IsEditorMode;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;
        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;

        var hasLeftSidebar = _states.LeftSidebarState != null;
        var leftHeight = hasLeftSidebar ? height : 52;

        _panelSize = new PanelSize
        {
            LeftSize = new Vector2(left, leftHeight),
            LeftPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight },
            RightSize = new Vector2(right, height),
            RightPosition = new Vector2(vp.WorkPos.X + vp.WorkSize.X - right, vp.WorkPos.Y + GuiTheme.TopbarHeight)
        };


        ConsoleComponent.CalculateSize(left, right);
    }

    public void OnDiagnosticTick()
    {
        var states = _states;
        if (states.ModeState.IsActive)
        {
            states.LeftSidebarState?.UpdateDiagnostic();
            states.RightSidebarState?.UpdateDiagnostic();
        }
        
        ConsoleGateway.Service.OnTick();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefreshData()
    {
        var states = _states;
        if (!states.ModeState.IsActive || !_refreshStepper.Tick()) return;
        states.LeftSidebarState?.Update();
        states.RightSidebarState?.Update();
        
        _stateHub.DrainQueue(_globalContext);
    }
}