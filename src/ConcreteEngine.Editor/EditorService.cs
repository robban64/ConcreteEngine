using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor;

internal sealed class EditorService
{
    private const int UpdateInterval = 4;
    private static readonly NativeArray<byte> TextBuffer = NativeArray.Allocate<byte>(256);

    internal static UnsafeSpanWriter GetWriter() => new(TextBuffer);

    private readonly InteractionHandler _interactionHandler;
    private readonly SelectionManager _selectionManager;

    private readonly PanelState _panelState;
    private readonly EventManager _eventManager;
    private readonly EditorEventHandler _eventHandler;

    private readonly ConsolePanel _console;
    private readonly ConsoleService _consoleService = ConsoleGateway.Service;

    private readonly WindowLayout _windowLayout;

    private FrameStepper _updateStepper = new(UpdateInterval);

    public EditorService(EngineController controller, GfxContext gfxContext)
    {
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

    public void Draw()
    {
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();

        GuiTheme.PushFontText();

        var ctx = new FrameContext(TextBuffer);
        _windowLayout.DrawLayout(ctx);
        _console.DrawConsole(_consoleService, ctx);
        _windowLayout.DrawPanels(ctx);

        _interactionHandler.Update();

        _eventManager.DrainQueue();

        ImGui.PopFont();
    }

    public void OnDiagnosticTick()
    {
        MetricSystem.Instance.TickDiagnostic();
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick(new FrameContext(TextBuffer));
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
    
    public static unsafe void DrawGizmos(InspectSceneObject inspector)
    {
        //var entity = inspector.SceneObject.GetRenderEntities()[0];
        Matrix4x4* matrices = stackalloc Matrix4x4[3];
        var view = &matrices[0];
        var proj = &matrices[1];
        var model = &matrices[2];

        *view = EngineObjects.Camera.ViewMatrix;
        *proj = EngineObjects.Camera.ProjectionMatrix;
        MatrixMath.CreateModelMatrix(in inspector.SceneObject.GetTransform(), out *model);

        var changed = ImGuizmo.Manipulate(
            &view->M11,
            &proj->M11,
            EditorInputState.GizmoOperation,
            EditorInputState.GizmoMode,
            &model->M11
        );

        if (changed)
        {
            Transform.FromMatrix(in *model, out var transform);
            inspector.SceneObject.SetTransform(in transform);
        }
    }
}