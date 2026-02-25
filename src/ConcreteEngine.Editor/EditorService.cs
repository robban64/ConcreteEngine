using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;

    private readonly GfxContext _gfxContext;

    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private FrameStepper _updateStepper = new(UpdateInterval);

    private static readonly NativeArray<byte> TextBuffer = new(256);

    public EditorService(EngineController controller, GfxContext gfxContext)
    {
        _gfxContext = gfxContext;

        _eventManager = new EventManager();
        _console = new ConsolePanel();
        _consoleService.Console = _console;

        _selectionManager = new SelectionManager(controller.AssetController, controller.SceneController);

        var panelContext = new PanelContext(_eventManager, _selectionManager, gfxContext.ResourceManager.GetGfxApi());
        _panelState = new PanelState(controller, panelContext);

        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState);

        _windowLayout = new WindowLayout(stateContext);
        _inputHandler = new InputHandler(controller.InteractionController, stateContext);
        _eventHandler = new EditorEventHandler(stateContext, controller);

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetSelectionEvent>(_eventHandler.OnAssetSelectionEvent);

        _eventManager.Register<AssetUpdateEvent>(EditorEventHandler.OnAssetUpdateEvent);

        ConsoleService.PrintCommands();
    }


    public void Update()
    {
        _inputHandler.UpdateMouse();
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();
    }

    public void Draw()
    {
        GuiTheme.PushFontText();

        var ctx = new FrameContext(TextBuffer);

        _windowLayout.DrawLayout(in ctx);
        _console.DrawConsole(_consoleService, in ctx);
        _windowLayout.DrawPanels(in ctx);

        ImGui.PopFont();

        _eventManager.DrainQueue();
    }

    public void OnDiagnosticTick()
    {
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick(new FrameContext(TextBuffer));
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
}