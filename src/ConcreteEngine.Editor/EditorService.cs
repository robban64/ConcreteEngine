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

internal sealed class EditorService
{
    public bool IsDiagnosticTick { get; set; }
    private bool _wasDiagnosticTick = false;

    private readonly StateManager _stateManager;
    private readonly WindowManager _windowManager;

    private readonly ConsoleService _consoleService;
    private readonly InteractionHandler _interactionHandler;
    
    private readonly EventDispatcher _eventDispatcher;

    private readonly PanelRouter _router;
    private readonly SelectionManager _selectionManager;


    public EditorService(GfxResourceApi gfxApi)
    {
        _consoleService = ConsoleGateway.Service;

        _eventDispatcher = new EventDispatcher();

        _stateManager = new StateManager(_eventDispatcher, gfxApi);

        _selectionManager = new SelectionManager(_stateManager);
        _interactionHandler = new InteractionHandler(_stateManager, _selectionManager);

        _windowManager = new WindowManager(_stateManager);
        _router = new PanelRouter(_stateManager, _windowManager);

        _consoleService.Setup();
        RegisterEvents();

        _windowManager.Init(_consoleService);
        _router.ForceResolve(_stateManager);

        ConsoleGateway.LogPlain($"PersistentArena: {TextBuffers.PersistentArena.Remaining} bytes left");
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
        _windowManager.Draw();
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

}