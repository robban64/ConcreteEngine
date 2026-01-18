using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int RefreshInterval = 4;

    private FrameStepper _refreshStepper = new(RefreshInterval);

    private readonly byte[] _buffer = new byte[512];

    private readonly ComponentHub _stateHub;
    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly StateContext _stateContext;
    private readonly EditorState _editorState;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly Layout _layout;


    public EditorService()
    {
        _stateHub = new ComponentHub();
        _selectionManager = new SelectionManager();
        _editorState = new EditorState(_stateHub);
        _stateContext = new StateContext(_stateHub, _selectionManager,_editorState);

        _inputHandler = new InputHandler(_stateContext);
        _console = new ConsoleComponent();
        _consoleService.Console = _console;


        _layout = new Layout(_stateContext);
    }

    public void UpdateStyle() => RefreshStyle();

    public void Initialize()
    {
        _stateHub.Initialize(_stateContext);
        EditorInput.Initialize(_inputHandler);
    }


    public void Render(float delta)
    {
        PrepareFrame(delta);
        
        //if (_states.ModeState.Mode == ViewMode.None) return;
        
        if (_refreshStepper.Tick()) _editorState.Update();
        
        var ctx = new FrameContext(new SpanWriter(_buffer), _stateContext, delta);
        _layout.DrawTop();
        _layout.DrawLeft(_editorState.Left, ctx);
        _layout.DrawRight(_editorState.Right, ctx);
        _console.DrawConsole(_consoleService, ctx);
        
        _stateHub.Update(_stateContext);
    }


    private void PrepareFrame(float delta)
    {
        if (!ImGuiController.IsBlockInput)
        {
            if (!ImGuiController.IsMouseOverEditor)
                EditorInput.UpdateMouse(delta);

            EditorInput.CheckHotkeys();
        }

        UpdateStyle();
        //if (_states.CommitState()) 
    }


    public void OnDiagnosticTick()
    {
        _editorState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    
    private void RefreshStyle()
    {
        var vp = ImGui.GetMainViewport();

        var isEditor = true;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        _console.CalculateSize(left, right);

        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = true;//stateHub.LeftSidebarState != null;
        var leftHeight = hasLeftSidebar ? height : 52;

        _layout.PanelSize = new PanelSize
        {
            LeftSize = new Vector2(left, leftHeight),
            LeftPosition = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight },
            RightSize = new Vector2(right, height),
            RightPosition = new Vector2(vp.WorkPos.X + vp.WorkSize.X - right, vp.WorkPos.Y + GuiTheme.TopbarHeight)
        };
    }
}