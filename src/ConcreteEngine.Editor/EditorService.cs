using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private readonly Topbar _topbar;
    private readonly PanelState _panelState;
    private readonly StateContext _stateContext;
    private readonly ConsoleService _consoleService;

    private readonly InteractionHandler _interactionHandler;

    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;
    private static ConsoleService ConsoleService;

    public EditorService(GfxContext gfxContext)
    {
        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        ConsoleService = _consoleService = ConsoleGateway.Service;

        _eventManager = new EventManager();
        _panelState = new PanelState(_consoleService);

        _stateContext = new StateContext(_eventManager, new SelectionManager(), _panelState, gfxApi);

        _topbar = new Topbar(_stateContext);
        _interactionHandler = new InteractionHandler(_stateContext);
        _eventHandler = new EditorEventHandler(_stateContext);

        _panelState.Register(_stateContext);

        _consoleService.Setup();
        RegisterEvents();

        ConsoleService.PrintCommands();
        ConsoleGateway.LogPlain("PersistentArena: " + TextBuffers.PersistentArena.Remaining + " bytes left");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private unsafe void RegisterEvents()
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
        WindowLayout.DrawConsole(_panelState);

        _interactionHandler.DrawGizmo();
        _eventManager.DrainQueue();

        ImGui.PopFont();
    }

    public void DiagnosticTick()
    {
        _consoleService.OnTick();
        _panelState.UpdateDiagnostic();
    }

    public void UpdateStyle() => WindowLayout.CalculatePanelSize(_panelState.LeftPanelId, _panelState.RightPanelId);
}