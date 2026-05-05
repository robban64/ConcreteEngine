using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Resources;
using Hexa.NET.ImGui;
using EventHandler = ConcreteEngine.Editor.Core.EventHandler;

namespace ConcreteEngine.Editor;

internal sealed class EditorManagerContext
{
    public readonly StateManager StateManager;
    public readonly SelectionManager SelectionManager;
    public readonly EventDispatcher EventDispatcher;
    public readonly WindowManager WindowManager;
    public readonly PanelRouter Router;

    public EditorManagerContext(GfxContext gfxContext)
    {
        EventDispatcher = new EventDispatcher();
        StateManager = new StateManager(EventDispatcher, gfxContext.ResourceManager.GetGfxApi());

        SelectionManager = new SelectionManager(StateManager);
        WindowManager = new WindowManager(StateManager);
    }
}

internal sealed class EditorService
{
    public bool IsDirty { get; set; } = true;
    public bool IsDiagnosticTick { get; set; } 
    private bool _wasDiagnosticTick = false;

    private readonly StateManager _stateManager;
    private readonly SelectionManager _selectionManager;
    private readonly EventDispatcher _eventDispatcher;
    private readonly WindowManager _windowManager;
    private readonly PanelRouter _router;

    private readonly ConsoleService _consoleService;
    private readonly InteractionHandler _interactionHandler;

    public EditorService(GfxResourceApi gfxApi)
    {
        _consoleService = ConsoleGateway.Service;

        _eventDispatcher = new EventDispatcher();

        _stateManager = new StateManager(_eventDispatcher, gfxApi);

        _windowManager = new WindowManager(_stateManager);
        _router = new PanelRouter(_stateManager, _windowManager);

        _selectionManager = new SelectionManager(_stateManager);
        _interactionHandler = new InteractionHandler(_stateManager, _selectionManager);

        _consoleService.Setup();
        RegisterEvents();

        _windowManager.Init(_stateManager, _consoleService);
        _router.ForceResolve(_stateManager);

        ConsoleGateway.LogPlain("PersistentArena: " + TextBuffers.PersistentArena.Remaining + " bytes left");
    }

    private void RegisterEvents()
    {
        _eventDispatcher.Register<SceneObjectEvent>(EventHandler.OnSceneObjectEvent);
        _eventDispatcher.Register<AssetEvent>(EventHandler.OnAssetUpdateEvent);

        _eventDispatcher.Register<SelectionEvent>(EventHandler.OnSelectionEvent);
        _eventDispatcher.Register<ToolEvent>(EventHandler.OnToolEvent);
        _eventDispatcher.Register<ModeEvent>(EventHandler.OnModeEvent);
    }

    public void Draw()
    {
        _interactionHandler.Update();

        GuiTheme.PushFontText();
        _windowManager.Draw(_interactionHandler);
        ImGui.PopFont();

        _eventDispatcher.DrainQueue(_stateManager);

        if (IsDiagnosticTick)
        {
            _consoleService.OnTick();
            IsDiagnosticTick = false;
            _wasDiagnosticTick = true;
        }

        if (_wasDiagnosticTick)
        {
            _windowManager.UpdateDiagnostic();
            _wasDiagnosticTick = false;
        }
    }


    public void UpdateLayout(out ViewportRect vp)
    {
        var left = _windowManager.GetWindow(WindowId.Left).Layout;
        var right = _windowManager.GetWindow(WindowId.Right).Layout;
        var bottom = _windowManager.GetWindow(WindowId.Bottom).Layout;
        WindowLayout.CalculateLayout(left, right, bottom);
        WindowLayout.CalculateViewport(out vp);
    }
}