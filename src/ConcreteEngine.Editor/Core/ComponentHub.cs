using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Bridge;
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
        if (!_dict.TryGetValue(typeof(TState), out var runtime)) return;
        runtime.TriggerEvent(eventKey, evt);
    }

    public void Initialize(GlobalContext ctx)
    {
        HasInit = true;
        ComponentRuntime.SetupDispatcher = _dispatcher;
        ComponentRuntime.SetupContext = ctx;

        Register<EmptyState>(RegisterMetrics());
        Register<SceneState>(RegisterSceneState());
        Register<AssetState>(RegisterAssetState());
        Register<SlotState<EditorCameraState>>(RegisterCameraState());
        Register<SlotState<EditorVisualState>>(RegisterVisualState());
        
        SceneRuntime.Enter();

        ComponentRuntime.SetupDispatcher = null;
        ComponentRuntime.SetupContext = null;
    }

    private void Register<TState>(ComponentRuntime runtime) where TState : class
    {
        _dict.Add(typeof(TState), runtime);
        _list.Add(runtime);
    }

    private ComponentRuntime RegisterMetrics()
    {
        return MetricsRuntime = ComponentRuntime
            .CreateBuilder<EmptyState, MetricsComponent>()
            .OnEnter(static (ctx, component, state) => MetricsApi.EnterMetricMode())
            .OnLeave(static (ctx, component, state) => MetricsApi.LeaveMetricMode())
            .OnDiagnostic(static (_,__, ___) => MetricsApi.Tick())
            .Build();
    }


    private ComponentRuntime RegisterSceneState()
    {
        return SceneRuntime = ComponentRuntime
            .CreateBuilder<SceneState, SceneComponent>()
            .OnEnter(static (ctx, component, state) => state.Proxy = ctx.SelectedProxy)
            .OnLeave(static (ctx, component, state) => { })
            .OnUpdate(static (ctx, component, state) =>
            {
                if(state.Proxy is not {} proxy) return;
                foreach (var property in proxy.Properties)
                {
                    property.Refresh();
                }
            })
            .RegisterEvent<SceneObjectId>(EventKey.SelectionChanged, static (ctx, state, evt) =>
            {
                if (ctx.SelectedId == evt) return;
                if (evt.IsValid()) ctx.SelectSceneObject(evt);
                else ctx.DeSelectSceneObject();
                ctx.EditorState.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.EditorState.SetRightSidebarState(RightSidebarMode.Property);
                
                state.Proxy = ctx.SelectedProxy;
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
                state.FileSpecs = ctx.AssetController.FetchAssetFileSpecs(evt);
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
            .Build();
    }

    private ComponentRuntime RegisterVisualState()
    {
        return VisualRuntime = ComponentRuntime
            .CreateBuilder<SlotState<EditorVisualState>, VisualParamComponent>()
            .OnEnter(static (ctx, component, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnUpdate(static (ctx, component, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave(static (ctx, component, state) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, static (ctx, state, evt) =>
            {
                var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(evt));
                CommandDispatcher.InvokeEditorCommand(payload);
            })
            .Build();
    }
/*
    private  void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter( (ctx, state) => { })
            .OnRefresh( (_) => { })
            .OnLeave( (ctx) => ctx.TriggerEvent<ISceneObject?>(EventKey.SelectionChanged, null))
            .RegisterEvent<ISceneObject?>(EventKey.SelectionChanged, OnEntitySelected)
            .Build();

        return;

         void OnEntitySelected(ISceneObject? it)
        {
            var entity = it?.Id ?? default;
            if (EditorDataStore.SelectedSceneObj == entity) return;
            if (entity.IsValid())
                EngineController.SelectEntity(entity);
            else
                EngineController.DeSelectEntity();

            if (!entity.IsValid()) return;
            StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
        }
    }*/
}