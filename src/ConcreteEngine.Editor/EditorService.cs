using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;
    
    private readonly InteractionHandler _interactionHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private FrameStepper _updateStepper = new(UpdateInterval);

    public EditorService(EngineController controller, GfxContext gfxContext)
    {
        _eventManager = new EventManager();
        _console = new ConsolePanel();
        _consoleService.Console = _console;

        _panelState = new PanelState();

        _selectionManager = new SelectionManager(controller.AssetController, controller.SceneController);

        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState, gfxApi);

        _windowLayout = new WindowLayout(stateContext);
        _interactionHandler = new InteractionHandler(controller.InteractionController, stateContext);
        _eventHandler = new EditorEventHandler(stateContext, controller);

        _panelState.Register(controller, stateContext);
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetSelectionEvent>(_eventHandler.OnAssetSelectionEvent);

        _eventManager.Register<AssetUpdateEvent>(EditorEventHandler.OnAssetUpdateEvent);

        ConsoleService.PrintCommands();
    }

    public void Draw()
    {
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();
        _interactionHandler.Update();

        GuiTheme.PushFontText();

        _windowLayout.DrawLayout();
        
        var ctx = new FrameContext(TextBuffers.GetWriter());
        _windowLayout.DrawPanels(ctx);
        _console.DrawConsole(_consoleService, in ctx);


        _eventManager.DrainQueue();

        ImGui.PopFont();
    }

    public void OnDiagnosticTick()
    {
        MetricSystem.Instance.TickDiagnostic();
        _panelState.UpdateDiagnostic();
        _console.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
    
}