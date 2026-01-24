using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;

    private FrameStepper _updateStepper = new(UpdateInterval);

    private readonly byte[] _buffer = new byte[512];

    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly Layout _layout;

    public EditorService(EngineController controller)
    {
        _eventManager = new EventManager();
        _console = new ConsoleComponent();
        _consoleService.Console = _console;

        _selectionManager = new SelectionManager(controller.AssetController, controller.SceneController);

        var panelContext = new PanelContext(_eventManager, _selectionManager);
        _panelState = new PanelState(controller, panelContext);

        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState);

        _layout = new Layout(stateContext);
        _inputHandler = new InputHandler(controller.InteractionController, stateContext);
        _eventHandler = new EditorEventHandler(stateContext, controller);
    }

    public void Initialize()
    {
        EditorInput.Initialize(_inputHandler);

        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSelectSceneObject);
        _eventManager.Register<AssetEvent>(_eventHandler.OnSelectAsset);
        _eventManager.Register<WorldEvent>(_eventHandler.OnCameraCommit);
        _eventManager.Register<VisualDataEvent>(_eventHandler.OnVisualCommit);

        _eventManager.Register<AssetReloadEvent>(static (evt) => EditorEventHandler.OnReloadAsset(evt));
        _eventManager.Register<GraphicsSettingsEvent>(static (evt) => EditorEventHandler.OnGraphicsSettings(evt));
    }

    private static void UpdateInput(float delta)
    {
        if (ImGuiController.IsBlockInput) return;

        if (!ImGuiController.IsMouseOverEditor)
            EditorInput.UpdateMouse(delta);

        EditorInput.CheckHotkeys();
    }

    public void Render(float delta)
    {
        UpdateInput(delta);

        var selection = _selectionManager;
        var panelState = _panelState;

        if (panelState.ClearDirty()) UpdateStyle();
        
        if (_updateStepper.Tick()) panelState.Update();
        _layout.DrawTop();

        var ctx = new FrameContext(_buffer, delta, selection.SelectedSceneId, selection.SelectedAssetId);
        _layout.DrawLeft(panelState.Left, in ctx);

        _layout.DrawRight(panelState.Right, in ctx);
        _console.DrawConsole(_consoleService, in ctx);

        _eventManager.DrainQueue();
    }


    public void OnDiagnosticTick()
    {
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    public void UpdateStyle()
    {
        var vp = ImGui.GetMainViewport();

        var isEditor = _panelState.RightPanelId != PanelId.MetricsRight;
        var left = isEditor ? GuiTheme.LeftSidebarDefaultWidth : GuiTheme.LeftSidebarCompactWidth;
        var right = isEditor ? GuiTheme.RightSidebarDefaultWidth : GuiTheme.RightSidebarCompactWidth;

        _console.CalculateSize(left, right);

        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var hasLeftSidebar = _panelState.Left != null;
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