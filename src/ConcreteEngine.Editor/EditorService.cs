using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Editor.Bridge;
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
    private const int UpdateInterval = 4;
    
    private readonly InteractionHandler _interactionHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private FrameStepper _updateStepper = new(UpdateInterval);

    private readonly SceneController _sceneController;
    private readonly AssetController _assetController;
    public EditorService(EngineController controller, GfxContext gfxContext)
    {
        _sceneController =  controller.SceneController;
        _assetController = controller.AssetController;
        _eventManager = new EventManager();
        _console = new ConsolePanel();
        _consoleService.Console = _console;

        _panelState = new PanelState();

        _selectionManager = new SelectionManager(controller.AssetController, controller.SceneController);

        var gfxApi = gfxContext.ResourceManager.GetGfxApi();
        var stateContext = new StateContext(_eventManager, _selectionManager, _panelState, gfxApi);

        _windowLayout = new WindowLayout(stateContext);
        _interactionHandler = new InteractionHandler(controller.InteractionController, stateContext);
        _eventHandler = new EditorEventHandler(stateContext, controller);

        _panelState.Register(controller, stateContext);
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventManager.Register<SceneObjectEvent>(_eventHandler.OnSceneObjectEvent);
        _eventManager.Register<AssetSelectionEvent>(_eventHandler.OnAssetSelectionEvent);

        _eventManager.Register<AssetUpdateEvent>(EditorEventHandler.OnAssetUpdateEvent);

        ConsoleService.PrintCommands();
    }

    private int _nameTick = 1;
    public void Draw()
    {
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();
        _interactionHandler.Update();

        GuiTheme.PushFontText();

        //TEMP
        if (_selectionManager.SelectedAsset is {} asset && asset.Asset is Model model && ImGui.IsKeyReleased(ImGuiKey.Space))
        {
            var materials = model.DefaultMaterials.Length > 0
                ? new MaterialId[model.DefaultMaterials.Length]
                : [new MaterialId(1)];

            if (materials.Length > 1)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    _assetController.TryGetByGuid<Material>(model.DefaultMaterials[i], out var material);
                    materials[i] = material.MaterialId;
                }
            }
            _sceneController.AddSceneObject(new SceneObjectTemplate
            {
                Name = $"spawner-{_nameTick++}",
                Blueprints = {new ModelBlueprint(model.Id,materials)},
                Transform = new Transform(EditorCamera.Instance.Camera.Translation, Vector3.One, Quaternion.Identity)
            });
        }

        _windowLayout.DrawLayout();
        
        var ctx = new FrameContext(TextBuffers.GetWriter());
        _windowLayout.DrawPanels(ctx);
        _console.DrawConsole(_consoleService, in ctx);

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