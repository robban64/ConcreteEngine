using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;

namespace ConcreteEngine.Editor.Core;


internal sealed class ComponentHub
{
    public bool HasInit { get; private set; }

    public ComponentRuntime MetricsRuntime { get; private set; } = null!;
    public ComponentRuntime SceneRuntime { get; private set; } = null!;
    public ComponentRuntime AssetRuntime { get; private set; } = null!;
    public ComponentRuntime WorldRuntime { get; private set; } = null!;
    public ComponentRuntime VisualRuntime { get; private set; } = null!;

    public ComponentRuntime? LeftSidebarState;
    public ComponentRuntime? RightSidebarState;

    private readonly Dictionary<Type, ComponentRuntime> _dict = new(8);
    private readonly List<ComponentRuntime> _list = new(8);

    private readonly DeferredEventDispatcher _dispatcher = new();

    public void Update(StateContext ctx)
    {
        LeftSidebarState?.Update();
        RightSidebarState?.Update();
        DrainQueue(ctx);
    }

    public void UpdateDiagnostic()
    {
        LeftSidebarState?.UpdateDiagnostic();
        RightSidebarState?.UpdateDiagnostic();
    }

    public void DrainQueue(StateContext ctx) => _dispatcher.Drain(ctx);

    public void TriggerEvent<TEvent>(TEvent evt) where TEvent : ComponentEvent
    {
        if (!_dict.TryGetValue(evt.ComponentType, out var runtime))
        {
            ConsoleGateway.Log(new StringLogEvent(LogScope.Editor,
                $"Invalid Event type for {evt.EventKey}, invoked with {typeof(TEvent).Name}"));
            return;
        }

        runtime.TriggerEvent(evt);
    }

    public void Initialize(StateContext ctx)
    {
        HasInit = true;
        ComponentRuntime.SetupDispatcher = _dispatcher;
        ComponentRuntime.SetupContext = ctx;

        Register<MetricsComponent>(RegisterMetrics());
        Register<SceneComponent>(RegisterSceneState());
        Register<AssetsComponent>(RegisterAssetState());
        Register<WorldComponent>(RegisterWorldState());
        Register<VisualComponent>(RegisterVisualState());

        SceneRuntime.Enter();

        ComponentRuntime.SetupDispatcher = null;
        ComponentRuntime.SetupContext = null;
    }

    private void Register<TComponent>(ComponentRuntime runtime) where TComponent : class
    {
        _dict.Add(typeof(TComponent), runtime);
        _list.Add(runtime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentRuntime? GetLeftTransition(LeftSidebarMode mode)
    {
        return mode switch
        {
            LeftSidebarMode.Metrics => MetricsRuntime,
            LeftSidebarMode.Scene => SceneRuntime,
            LeftSidebarMode.Assets => AssetRuntime,
            _ => null
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentRuntime? GetRightTransition(RightSidebarMode mode)
    {
        return mode switch
        {
            RightSidebarMode.AssetProperty => AssetRuntime,
            RightSidebarMode.SceneProperty => SceneRuntime,
            RightSidebarMode.Metrics => MetricsRuntime,
            RightSidebarMode.World => WorldRuntime,
            RightSidebarMode.Visuals => VisualRuntime,
            _ => null
        };
    }

    private ComponentRuntime RegisterMetrics()
    {
        return MetricsRuntime = ComponentRuntime
            .CreateBuilder< MetricsComponent>()
            .OnEnter(static (ctx, component) => MetricsApi.EnterMetricMode())
            .OnLeave(static (ctx, component) => MetricsApi.LeaveMetricMode())
            .OnDiagnostic(static (_, __) => MetricsApi.Tick())
            .Build(new MetricsComponent());
    }


    private ComponentRuntime RegisterSceneState()
    {
        return SceneRuntime = ComponentRuntime
            .CreateBuilder< SceneComponent>()
            .OnEnter(static (ctx, component) =>
            {
                component.State.Proxy = ctx.Selection.SceneProxy;
            })
            .OnLeave(static (ctx, component) => { })
            .OnUpdate(static (ctx, component) =>
            {
                var state = component.State;
                
                if (state.Proxy is not { } proxy) return;
                foreach (var property in proxy.Properties)
                    property.Refresh();

                state.Fill(CollectionsMarshal.AsSpan(proxy.Properties));
                state.PreviousId = state.SelectedId;
            })
            .RegisterEvent<SceneObjectEvent>(EventKey.SelectionChanged, static (ctx,  evt) =>
            {
                if (ctx.Selection.SelectedSceneId == evt.SceneObject) return;
                if (evt.SceneObject.IsValid()) ctx.Selection.SelectSceneObject(evt.SceneObject);
                else ctx.Selection.DeSelectSceneObject();
                ctx.StateManager.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.StateManager.SetRightSidebarState(RightSidebarMode.SceneProperty);
            })
            .Build(new SceneComponent());
    }


    private ComponentRuntime RegisterAssetState()
    {
        return AssetRuntime = ComponentRuntime
            .CreateBuilder< AssetsComponent>()
            .OnEnter(static (ctx, component) => { })
            .OnLeave(static (ctx, component) =>
            {
                //component.State.ResetState();
                //ctx.Selection.DeselectAsset();
            })
            .RegisterEvent<AssetEvent>(EventKey.SelectionChanged, static (ctx,  evt) =>
            {
                if (ctx.Selection.SelectedAssetId == evt.Asset) return;
                if (!evt.Asset.IsValid())
                {
                    ctx.Selection.DeselectAsset();
                    return;
                }

                ctx.Selection.SelectAsset(evt.Asset);
                ctx.StateManager.SetLeftSidebarState(LeftSidebarMode.Assets);
                ctx.StateManager.SetRightSidebarState(RightSidebarMode.AssetProperty);
            })
            .RegisterEvent<AssetEvent>(EventKey.SelectionAction, static (ctx, evt) =>
            {
                ArgumentException.ThrowIfNullOrEmpty(evt.Name);
                var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, evt.Name);
                CommandDispatcher.InvokeEditorCommand(cmd);
            })
            .Build(new AssetsComponent());
    }


    private ComponentRuntime RegisterWorldState()
    {
        return WorldRuntime = ComponentRuntime
            .CreateBuilder<WorldComponent>()
            .OnEnter(static (ctx, component) =>
            {
                EngineController.WorldController.FetchCamera(component.State.CameraState.GetView());
            })
            .OnUpdate(static (ctx, component) =>
            {
                if (component.State.Selection == WorldSelection.Camera)
                    EngineController.WorldController.FetchCamera(component.State.CameraState.GetView());
            })
            .OnLeave(static (ctx, component) => { })
            .RegisterEvent<WorldEvent>(EventKey.CategoryChanged, static (ctx,  evt) => {})
            .RegisterEvent<WorldEvent>(EventKey.CommitData, static (ctx,  evt) =>
            {
                if (evt.State.Selection == WorldSelection.Camera)
                    EngineController.WorldController.CommitCamera(evt.State.CameraState.GetView());
            })
            .Build(new WorldComponent());
    }

    private ComponentRuntime RegisterVisualState()
    {
        return VisualRuntime = ComponentRuntime
            .CreateBuilder< VisualComponent>()
            .OnEnter(static (ctx, component) =>
                EngineController.WorldController.FetchVisualParams(component.State.GetView()))
            .OnUpdate(static (ctx, component) =>
                EngineController.WorldController.FetchVisualParams(component.State.GetView()))
            .OnLeave(static (ctx, component) => { })
            .RegisterEvent<VisualDataEvent>(EventKey.CommitData, static (ctx, evt) =>
            {
                EngineController.WorldController.CommitVisualParams(evt.State.GetView());
            })
            .RegisterEvent<GraphicsSettingsEvent>(EventKey.GraphicsSetting, static (ctx, evt) =>
            {
                if (evt.ShadowSize is { } shadowSize)
                {
                    var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(shadowSize));
                    CommandDispatcher.InvokeEditorCommand(payload);
                }
            })
            .Build(new VisualComponent());
    }
}