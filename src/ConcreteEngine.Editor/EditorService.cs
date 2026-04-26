using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private readonly StateManager _stateManager;
    private readonly ConsoleService _consoleService;
    private readonly InteractionHandler _interactionHandler;
    private readonly EventManager _eventManager;
    private readonly WindowManager _windowManager;
    private readonly PanelRouter _router;

    private bool _firstTick = false;

    public EditorService(GfxContext gfxContext)
    {
        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        _consoleService = ConsoleGateway.Service;

        _eventManager = new EventManager();

        _stateManager = new StateManager(_eventManager, new SelectionManager(), gfxApi);

        _interactionHandler = new InteractionHandler(_stateManager);
        _windowManager = new WindowManager(_stateManager);
        _router = new PanelRouter(_windowManager, _stateManager);
    
        _consoleService.Setup();
        RegisterEvents();
        
        _windowManager.Init(_stateManager, _consoleService);
        _router.ForceResolve(_stateManager);

        ConsoleService.PrintCommands();
        ConsoleGateway.LogPlain("PersistentArena: " + TextBuffers.PersistentArena.Remaining + " bytes left");
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SceneObjectEvent>(EditorEventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetEvent>(EditorEventHandler.OnAssetUpdateEvent);

        _eventManager.Register<SelectionEvent>(EditorEventHandler.OnSelectionEvent);
        _eventManager.Register<ToolEvent>(EditorEventHandler.OnToolEvent);
        _eventManager.Register<ModeEvent>(EditorEventHandler.OnModeEvent);
    }

    public void Draw()
    {
        if (_firstTick)
        {
            UpdateStyle();
            _firstTick = false;
        }

        _interactionHandler.Update();
        
        _windowManager.Draw();

        _interactionHandler.DrawGizmo();
        _eventManager.DrainQueue(_stateManager);

        ImGui.PopFont();
    }

    public void DiagnosticTick()
    {
        _consoleService.OnTick();
        _windowManager.UpdateDiagnostic();
    }

    public void UpdateStyle()
    {
        var left = _windowManager.GetWindow(WindowId.Left).Layout;
        var right = _windowManager.GetWindow(WindowId.Right).Layout;
        var bottom = _windowManager.GetWindow(WindowId.Bottom).Layout;
        WindowLayout.CalculateLayout(left, right, bottom);
    }
}