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

    public ComponentRuntime? LeftSidebarState;
    public ComponentRuntime? RightSidebarState;

    private readonly Dictionary<Type, ComponentRuntime> _dict = new(8);
    private readonly List<ComponentRuntime> _list = new(8);

    private readonly DeferredEventDispatcher _dispatcher = new();

    public ComponentRuntime Get(Type type) => _dict[type];
    public ComponentRuntime Get<T>() where T : EditorComponent => _dict[typeof(T)];

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


    public void Update(StateContext ctx)
    {
        _dispatcher.Drain(ctx);
    }

    public void UpdateDiagnostic()
    {
    }


    public void Initialize(StateContext ctx)
    {
        HasInit = true;
        ComponentRuntime.Dispatcher = _dispatcher;
        ComponentRuntime.StateContext = ctx;

        Register<MetricsComponent>(RegisterMetrics());
        Register<SceneComponent>(RegisterSceneState(ctx));
        Register<AssetsComponent>(RegisterAssetState());
        Register<WorldComponent>(RegisterWorldState());
        Register<VisualComponent>(RegisterVisualState());

        ctx.EmitTransition(TransitionMessage.PushLeft(typeof(AssetsComponent)));
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
            LeftSidebarMode.Metrics => Get<MetricsComponent>(),
            LeftSidebarMode.Scene => Get<SceneComponent>(),
            LeftSidebarMode.Assets => Get<AssetsComponent>(),
            _ => null
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentRuntime? GetRightTransition(RightSidebarMode mode)
    {
        return mode switch
        {
            RightSidebarMode.AssetProperty => Get<AssetsComponent>(),
            RightSidebarMode.SceneProperty => Get<SceneComponent>(),
            RightSidebarMode.Metrics => Get<MetricsComponent>(),
            RightSidebarMode.World => Get<WorldComponent>(),
            RightSidebarMode.Visuals => Get<VisualComponent>(),
            _ => null
        };
    }

    private ComponentRuntime RegisterMetrics()
    {
        return ComponentRuntime
            .CreateBuilder<MetricsComponent>()
            .OnEnter(static (ctx, component) => MetricsApi.EnterMetricMode())
            .OnLeave(static (ctx, component) => MetricsApi.LeaveMetricMode())
            .OnDiagnostic(static (_, __) => MetricsApi.Tick())
            .Build(new MetricsComponent());
    }


    private ComponentRuntime RegisterSceneState(StateContext ctx)
    {
        return ComponentRuntime
            .CreateBuilder<SceneComponent>()
            .OnEnter(static (ctx, component) =>
            {
                component.State.Selection = ctx.Selection;
            })
            .OnLeave(static (ctx, component) => { })
            .OnUpdate(static (ctx, component) =>
            {
                var state = component.State;
                if (ctx.Selection.SceneProxy is not { } proxy) return;
                foreach (var property in proxy.Properties)
                    property.Refresh();

                state.Fill(CollectionsMarshal.AsSpan(proxy.Properties));
                state.PreviousId = ctx.Selection.SelectedSceneId;
            })
            .RegisterEvent<SceneObjectEvent>(EventKey.SelectionChanged, static (ctx, evt) =>
            {
                if (ctx.Selection.SelectedSceneId == evt.SceneObject) return;
                if (!evt.SceneObject.IsValid())
                {
                    ctx.Selection.DeSelectSceneObject();
                    ctx.EmitTransition(TransitionMessage.PopRight(typeof(SceneComponent)));
                }

                ctx.Selection.SelectSceneObject(evt.SceneObject);
                ctx.EmitTransition(TransitionMessage.PushRight(typeof(SceneComponent)));
            })
            .Build(new SceneComponent());
    }


    private ComponentRuntime RegisterAssetState()
    {
        return ComponentRuntime
            .CreateBuilder<AssetsComponent>()
            .OnEnter(static (ctx, component) =>
            {
            })
            .OnLeave(static (ctx, component) =>
            {
                //component.State.ResetState();
                //ctx.Selection.DeselectAsset();
            })
            .RegisterEvent<AssetEvent>(EventKey.SelectionChanged, static (ctx, evt) =>
            {
                if (ctx.Selection.SelectedAssetId == evt.Asset) return;

                if (!evt.Asset.IsValid())
                {
                    ctx.Selection.DeselectAsset();
                    ctx.EmitTransition(TransitionMessage.PopRight(typeof(AssetsComponent)));
                    return;
                }

                ctx.Selection.SelectAsset(evt.Asset);
                ctx.EmitTransition(TransitionMessage.PushRight(typeof(AssetsComponent)));
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
        return ComponentRuntime
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
            .RegisterEvent<WorldEvent>(EventKey.CategoryChanged, static (ctx, evt) => { })
            .RegisterEvent<WorldEvent>(EventKey.CommitData, static (ctx, evt) =>
            {
                if (evt.State.Selection == WorldSelection.Camera)
                    EngineController.WorldController.CommitCamera(evt.State.CameraState.GetView());
            })
            .Build(new WorldComponent());
    }

    private ComponentRuntime RegisterVisualState()
    {
        return ComponentRuntime
            .CreateBuilder<VisualComponent>()
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