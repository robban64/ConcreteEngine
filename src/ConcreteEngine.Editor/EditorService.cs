using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;

    private FrameStepper _updateStepper = new(UpdateInterval);

    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private static readonly NativeArray<byte> TextBuffer = new(256, true);

    public EditorService(EngineController controller)
    {
        _eventManager = new EventManager();
        _console = new ConsoleComponent();
        _consoleService.Console = _console;

        _selectionManager = new SelectionManager(controller.AssetController, controller.SceneController);

        var panelContext = new PanelContext(_eventManager, _selectionManager);
        _panelState = new PanelState(controller, panelContext);

        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState);

        _windowLayout = new WindowLayout(stateContext);
        _inputHandler = new InputHandler(controller.InteractionController, stateContext);
        _eventHandler = new EditorEventHandler(stateContext, controller);

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSelectSceneObject);
        _eventManager.Register<AssetEvent>(_eventHandler.OnSelectAsset);
        _eventManager.Register<WorldEvent>(_eventHandler.OnCameraCommit);
        _eventManager.Register<VisualDataEvent>(_eventHandler.OnVisualCommit);

        _eventManager.Register<AssetReloadEvent>(static (evt) => EditorEventHandler.OnReloadAsset(evt));
        _eventManager.Register<GraphicsSettingsEvent>(static (evt) => EditorEventHandler.OnGraphicsSettings(evt));

        ConsoleService.PrintCommands();
    }

    private void Prepare()
    {
        _inputHandler.UpdateMouse();
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();
    }


    public void Render(float delta)
    {
        Prepare();
        
        var sw = new StrWriter8(TextBuffer);
        _windowLayout.Draw(sw);
        _console.DrawConsole(_consoleService, sw);
        
        var ctx = new FrameContext(in sw, delta, _selectionManager.SelectedSceneId, _selectionManager.SelectedAssetId);
        _windowLayout.DrawPanels(in ctx);
        _eventManager.DrainQueue();

    }


    public void OnDiagnosticTick()
    {
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
}