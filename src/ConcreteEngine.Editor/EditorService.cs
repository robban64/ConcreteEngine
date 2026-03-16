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
    private readonly InteractionHandler _interactionHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    public EditorService(GfxContext gfxContext)
    {
        _eventManager = new EventManager();
        _console = new ConsolePanel();
        _panelState = new PanelState();

        _selectionManager = new SelectionManager();

        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState, gfxApi);

        _windowLayout = new WindowLayout(stateContext);
        _interactionHandler = new InteractionHandler( stateContext);
        _eventHandler = new EditorEventHandler(stateContext);

        _panelState.Register(stateContext);
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SelectionEvent>(_eventHandler.OnSelectionEvent);
        _eventManager.Register<SceneObjectEvent>(EditorEventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetEvent>(EditorEventHandler.OnAssetUpdateEvent);

        ConsoleService.PrintCommands();
    }

    public void Draw()
    {
        if (_panelState.ClearDirty()) UpdateStyle();
        //_panelState.Update();
        _interactionHandler.Update();

        GuiTheme.PushFontText();

        _windowLayout.DrawLayout();
        _windowLayout.DrawPanels(new FrameContext(TextBuffers.GetWriter()));
        _console.DrawConsole(_consoleService);

        _interactionHandler.DrawGizmo();
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