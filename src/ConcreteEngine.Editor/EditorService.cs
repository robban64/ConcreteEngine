using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
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

    public EditorService()
    {
        _eventDispatcher = new EventDispatcher();

        _stateManager = new StateManager(_eventDispatcher);

        _selectionManager = new SelectionManager(_stateManager);
        _interactionHandler = new InteractionHandler(_stateManager, _selectionManager);

        _windowManager = new WindowManager(_stateManager);
        _router = new PanelRouter(_stateManager, _windowManager);

    }

    public void Setup()
    {
        RegisterEvents();

        _windowManager.Setup();
        _router.ForceResolve(_stateManager);

        ConsoleGateway.LogPlain($"PersistentArena: {TextBuffers.PersistentArena.Remaining} bytes left");
        ConsoleGateway.LogPlain($"StringArena: {StringArena.Remaining} bytes left");

    }

    private void RegisterEvents()
    {
        _eventDispatcher.Register<SceneObjectEvent>(EventHandler.OnSceneObjectEvent);
        _eventDispatcher.Register<AssetEvent>(EventHandler.OnAssetUpdateEvent);
        _eventDispatcher.Register<SelectionEvent>(EventHandler.OnSelectionEvent);
        _eventDispatcher.Register<ToolEvent>(EventHandler.OnToolEvent);
    }

    private static AvgFrameTimer _avg;

    public void Draw()
    {
        _avg.BeginSample();
        
        GuiTheme.PushFontText();
        _windowManager.Draw();
        ImGui.PopFont();
        
        if (_avg.EndSample() >= 80) _avg.ResetAndPrint("Editor.Draw");

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