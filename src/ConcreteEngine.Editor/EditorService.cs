using ConcreteEngine.Editor.App.CLI;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Logging;
using Hexa.NET.ImGui;
using EventHandler = ConcreteEngine.Editor.Core.EventHandler;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private readonly StateManager _stateManager;
    private readonly WindowManager _windowManager;
    private readonly InteractionHandler _interactionHandler;

    private readonly EventDispatcher _eventDispatcher;

    private readonly PanelRouter _router;
    private readonly SelectionManager _selectionManager;
    private readonly ConsoleSystem _cli;

    public EditorService()
    {
        _eventDispatcher = new EventDispatcher();
        _cli = new ConsoleSystem();
        _stateManager = new StateManager(_eventDispatcher);

        _selectionManager = new SelectionManager(_stateManager);
        _interactionHandler = new InteractionHandler(_stateManager, _selectionManager);

        _windowManager = new WindowManager(_stateManager);
        _router = new PanelRouter(_stateManager, _windowManager);

    }

    public void Setup()
    {
        RegisterEvents();
        RegisterCli();
        
        _windowManager.Setup();
        _router.ForceResolve(_stateManager);

        LogService.PushMessage($"StringArena: {StringArena.Remaining} bytes left");

    }
    
    private void RegisterCli()
    {
        _cli.RegisterCommand<UtilityCommandHandler>();
    }

    private void RegisterEvents()
    {
        _eventDispatcher.Register<SceneObjectEvent>(EventHandler.OnSceneObjectEvent);
        _eventDispatcher.Register<AssetEvent>(EventHandler.OnAssetUpdateEvent);
        _eventDispatcher.Register<SelectionEvent>(EventHandler.OnSelectionEvent);
        _eventDispatcher.Register<ToolEvent>(EventHandler.OnToolEvent);
    }
    
    public void Draw()
    {
        AppLayout.PushFontText();
        _windowManager.Draw();
        ImGui.PopFont();
        
        if (EditorInput.UpdateInputState(_selectionManager.HasSceneObject))
            EditorTime.WakeUp();

        _interactionHandler.Update();

        _eventDispatcher.DrainQueue(_stateManager);
    }

    public void OnDiagnosticTick()
    {
        _windowManager.OnDiagnosticTick();
    }
}