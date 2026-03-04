using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
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

    private readonly InputHandler _inputHandler;
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
        _inputHandler = new InputHandler(controller.InteractionController, stateContext);
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

    private static Matrix4x4 objMatrix;
    private void DrawGizmos()
    {
        if(_selectionManager.SelectedSceneObject is not {} inspector) return;
        var entity = inspector.SceneObject.GetRenderEntities()[0];

        ref var mat = ref objMatrix;
        MatrixMath.CreateModelMatrix(in inspector.SceneObject.GetTransform(), out mat);
        bool changed =ImGuizmo.Manipulate(
            ref Unsafe.AsRef(in EngineObjects.Camera.GetRenderViewMatrix()),
            ref Unsafe.AsRef(in EngineObjects.Camera.GetProjectionMatrix()),
            ImGuizmoOperation.Rotate,
            ImGuizmoMode.World,
            ref mat
        );
        if (changed)
        {
            Matrix4x4.Decompose(mat, out var scale, out var rot, out var pos);
            var transform = new Transform(in pos, in scale, in rot);
            inspector.SceneObject.SetTransform(in transform);
        }
    }

    public void Update()
    {
        _inputHandler.UpdateMouse();
        if (_panelState.ClearDirty()) UpdateStyle();
        if (_updateStepper.Tick()) _panelState.Update();
    }

    private AvgFrameTimer avg;
    public void Draw()
    {
        GuiTheme.PushFontText();

        var ctx = new FrameContext(TextBuffer);

        _windowLayout.DrawLayout(ctx);
        _console.DrawConsole(_consoleService, ctx);

        _windowLayout.DrawPanels(ctx);
avg.BeginSample();
        DrawGizmos();
        avg.EndSample();
        if(avg.Ticks >= 40) avg.ResetAndPrint(); 
        ImGui.PopFont();
        _eventManager.DrainQueue();
    }

    public void OnDiagnosticTick()
    {
        MetricSystem.Instance.TickDiagnostic();
        _panelState.UpdateDiagnostic();
        ConsoleGateway.Service.OnTick(new FrameContext(TextBuffer));
    }

    public void UpdateStyle() => _windowLayout.CalculatePanelSize();
}