using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Panels;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;

    private readonly GfxContext _gfxContext ;

    private readonly InputHandler _inputHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsoleComponent _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private FrameStepper _updateStepper = new(UpdateInterval);

    private static readonly NativeArray<byte> TextBuffer = new(256);


    public EditorService(EngineController controller, GfxContext gfxContext)
    {
        TextFieldFormatter.Sw = new UnsafeSpanWriter(in TextBuffer);

        _gfxContext = gfxContext;

        _eventManager = new EventManager();
        _console = new ConsoleComponent();
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
        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSelectSceneObject);
        _eventManager.Register<AssetEvent>(_eventHandler.OnSelectAsset);

        _eventManager.Register<AssetReloadEvent>(static (evt) => EditorEventHandler.OnReloadAsset(evt));

        ConsoleService.PrintCommands();
    }

    public void Update(float delta)
    {
        _inputHandler.UpdateMouse();
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();

        DurationProfileTimer.Default.Begin();

        GuiTheme.PushFontText();

        var ctx = new FrameContext(in TextBuffer, delta, _selectionManager.SelectedSceneId,
            _selectionManager.SelectedAssetId);

        _windowLayout.Draw(in ctx);
        _console.DrawConsole(_consoleService, in ctx);
        _windowLayout.DrawPanels(in ctx);

        _eventManager.DrainQueue();
        
        ImGui.PopFont();
        DurationProfileTimer.Default.EndPrintSimple();

    }

    public void OnDiagnosticTick()
    {
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick();
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
}