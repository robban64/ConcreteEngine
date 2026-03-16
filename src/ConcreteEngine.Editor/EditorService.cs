using ConcreteEngine.Core.Diagnostics.Time;
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

    private readonly StateContext _stateContext;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly Topbar _topbar;

    public EditorService(GfxContext gfxContext)
    {
        _eventManager = new EventManager();
        _console = new ConsolePanel();
        _panelState = new PanelState();

        _selectionManager = new SelectionManager();

        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        var stateContext = _stateContext = new StateContext(_eventManager, _selectionManager, _panelState, gfxApi);

        _topbar = new Topbar(stateContext);
        _interactionHandler = new InteractionHandler(stateContext);
        _eventHandler = new EditorEventHandler(stateContext);

        _panelState.Register(stateContext);

        RegisterEvents();

        ConsoleService.PrintCommands();
        ConsoleGateway.LogPlain("PersistentArena: " + TextBuffers.PersistentArena.Remaining + "bytes left");
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SelectionEvent>(_eventHandler.OnSelectionEvent);
        _eventManager.Register<SceneObjectEvent>(EditorEventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetEvent>(EditorEventHandler.OnAssetUpdateEvent);
    }


    public void Draw()
    {
        if (_panelState.ClearDirty()) UpdateStyle();
        _interactionHandler.Update();

        GuiTheme.PushFontText();
        
        WindowLayout.DrawTopbar(_topbar);
        WindowLayout.DrawPanels(_panelState, _stateContext, new FrameContext(TextBuffers.GetWriter()));
        WindowLayout.DrawConsole(_console, _consoleService);

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

    public void UpdateStyle() => WindowLayout.CalculatePanelSize(_panelState.LeftPanelId, _panelState.RightPanelId);
}