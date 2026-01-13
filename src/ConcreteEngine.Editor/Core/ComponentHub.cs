using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;

namespace ConcreteEngine.Editor.Core;

internal sealed class EmptyState
{
    public static EmptyState Instance { get; } = new();
}

internal sealed class ComponentHub
{
    public bool HasInit { get; private set; }

    public ComponentRuntime MetricsRuntime { get; private set; } = null!;
    public ComponentRuntime SceneRuntime { get; private set; } = null!;
    public ComponentRuntime AssetRuntime { get; private set; } = null!;
    public ComponentRuntime CameraRuntime { get; private set; } = null!;
    public ComponentRuntime VisualRuntime { get; private set; } = null!;

    private readonly Dictionary<Type, ComponentRuntime> _dict = new(8);
    private readonly List<ComponentRuntime> _list = new(8);

    private readonly DeferredEventDispatcher _dispatcher = new();

    public void DrainQueue(GlobalContext ctx)
    {
        _dispatcher.Drain(ctx);
    }


    public void TriggerEvent<TState, TEvent>(EventKey eventKey, TEvent evt) where TState : class
    {
        if (!_dict.TryGetValue(typeof(TState), out var runtime))
        {
            ConsoleGateway.Log(new StringLogEvent(LogScope.Editor,
                $"Invalid Event type for {eventKey}, invoked with {typeof(TEvent).Name}"));
            return;
        }

        runtime.TriggerEvent(eventKey, evt);
    }

    public void Initialize(GlobalContext ctx)
    {
        HasInit = true;
        ComponentRuntime.SetupDispatcher = _dispatcher;
        ComponentRuntime.SetupContext = ctx;

        Register<MetricsComponent>(RegisterMetrics());
        Register<SceneComponent>(RegisterSceneState());
        Register<AssetsComponent>(RegisterAssetState());
        Register<CameraComponent>(RegisterCameraState());
        Register<VisualParamComponent>(RegisterVisualState());

        SceneRuntime.Enter();

        ComponentRuntime.SetupDispatcher = null;
        ComponentRuntime.SetupContext = null;
    }

    private void Register<TComponent>(ComponentRuntime runtime) where TComponent : class
    {
        _dict.Add(typeof(TComponent), runtime);
        _list.Add(runtime);
    }

    private ComponentRuntime RegisterMetrics()
    {
        return MetricsRuntime = ComponentRuntime
            .CreateBuilder<EmptyState, MetricsComponent>()
            .OnEnter(static (ctx, component, state) => MetricsApi.EnterMetricMode())
            .OnLeave(static (ctx, component, state) => MetricsApi.LeaveMetricMode())
            .OnDiagnostic(static (_, __, ___) => MetricsApi.Tick())
            .Build();
    }


    private ComponentRuntime RegisterSceneState()
    {
        return SceneRuntime = ComponentRuntime
            .CreateBuilder<SceneState, SceneComponent>()
            .OnEnter(static (ctx, component, state) =>
            {
                state.Proxy = ctx.Selection.Proxy;
                ctx.EditorState.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.EditorState.SetRightSidebarState(RightSidebarMode.Property);
            })
            .OnLeave(static (ctx, component, state) => { })
            .OnUpdate(static (ctx, component, state) =>
            {
                if (state.Proxy is not { } proxy) return;
                foreach (var property in proxy.Properties)
                    property.Refresh();

                state.Fill(CollectionsMarshal.AsSpan(proxy.Properties));
                state.PreviousId = state.SelectedId;
            })
            .RegisterEvent<SceneObjectId>(EventKey.SelectionChanged, static (ctx, state, evt) =>
            {
                if (ctx.Selection.SelectedId == evt) return;
                if (evt.IsValid()) ctx.Selection.SelectSceneObject(evt);
                else ctx.Selection.DeSelectSceneObject();
                ctx.EditorState.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.EditorState.SetRightSidebarState(RightSidebarMode.Property);

                state.Proxy = ctx.Selection.Proxy;
            })
            .Build();
    }


    private ComponentRuntime RegisterAssetState()
    {
        return AssetRuntime = ComponentRuntime
            .CreateBuilder<AssetState, AssetsComponent>()
            .OnEnter(static (ctx, component, state) => { })
            .OnLeave(static (ctx, component, state) => state.ResetState())
            .RegisterEvent<AssetId>(EventKey.SelectionChanged, static (ctx, state, evt) =>
            {
                state.FileSpecs = EngineController.AssetController.FetchAssetFileSpecs(evt);
            })
            .RegisterEvent<string>(EventKey.SelectionAction, static (ctx, state, evt) =>
            {
                var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, evt);
                CommandDispatcher.InvokeEditorCommand(cmd);
            })
            .Build();
    }


    private ComponentRuntime RegisterCameraState()
    {
        return CameraRuntime = ComponentRuntime
            .CreateBuilder<SlotState<EditorCameraState>, CameraComponent>()
            .OnEnter(static (ctx, component, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnUpdate(static (ctx, component, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnLeave(static (ctx, component, state) => { })
            .RegisterEvent<EmptyEvent>(EventKey.CommitVisualData, static (ctx, state, evt) =>
            {
                EngineController.WorldController.CommitCamera(state.GetView());
            })
            .Build();
    }

    private ComponentRuntime RegisterVisualState()
    {
        return VisualRuntime = ComponentRuntime
            .CreateBuilder<SlotState<EditorVisualState>, VisualParamComponent>()
            .OnEnter(static (ctx, component, state) =>
                EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnUpdate(static (ctx, component, state) =>
                EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave(static (ctx, component, state) => { })
            .RegisterEvent<EmptyEvent>(EventKey.CommitVisualData, static (ctx, state, evt) =>
            {
                EngineController.WorldController.CommitVisualParams(state.GetView());
            })
            .RegisterEvent<int>(EventKey.GraphicsSetting, static (ctx, state, evt) =>
            {
                var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(evt));
                CommandDispatcher.InvokeEditorCommand(payload);
            })
            .Build();
    }
}