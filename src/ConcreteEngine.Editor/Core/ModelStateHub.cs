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

internal sealed class ModelStateHub
{
    public bool HasInit { get; private set; }

    public ComponentRuntime MetricsRuntime { get; private set; } = null!;
    public ComponentRuntime SceneRuntime { get; private set; } = null!;
    public ComponentRuntime AssetRuntime { get; private set; } = null!;
    public ComponentRuntime CameraRuntime { get; private set; } = null!;
    public ComponentRuntime VisualRuntime { get; private set; } = null!;

    private readonly Dictionary<Type, ComponentRuntime> _dict = new (8);
    private readonly DeferredEventDispatcher _dispatcher = new();

    public void TriggerEvent<TState, TEvent>(EventKey eventKey, TEvent evt) where TState : class
    {
        if(!_dict.TryGetValue(typeof(TState), out var runtime)) return;
        runtime.TriggerEvent(eventKey,evt);
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

        ComponentRuntime.SetupDispatcher = null;
        ComponentRuntime.SetupContext = null;

        //SceneStateComponent.InvokeAction(TransitionKey.Enter);
    }

    private void Register<TState>(ComponentRuntime runtime)  where TState : class
    {
        _dict.Add(typeof(TState), runtime);
    }

    private ComponentRuntime RegisterMetrics()
    {
        return MetricsRuntime = ComponentRuntime
            .CreateBuilder<EmptyState, MetricsComponent>()
            .OnEnter((ctx, state) => MetricsApi.EnterMetricMode())
            .OnLeave((ctx, state) => MetricsApi.LeaveMetricMode())
            .Build();
    }


    private ComponentRuntime RegisterSceneState()
    {
        return SceneRuntime = ComponentRuntime
            .CreateBuilder<SceneState, SceneComponent>()
            .OnEnter((ctx, state) => { })
            .OnLeave((ctx, state) => { })
            .RegisterEvent<SceneObjectId>(EventKey.SelectionChanged, (ctx, state, evt) =>
            {
                if (ctx.SelectedId == evt) return;
                //if (evt.Msg.IsValid()) EngineController.SelectSceneObject(id);
                //else EngineController.DeSelectSceneObject();
                ctx.EditorState.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.EditorState.SetRightSidebarState(RightSidebarMode.Property);
            })
            .Build();
    }


    private ComponentRuntime RegisterAssetState()
    {
        return AssetRuntime = ComponentRuntime
            .CreateBuilder<AssetState, AssetsComponent>()
            .OnEnter((ctx, state) => {})
            .OnLeave((ctx, state) => state.ResetState())
            .RegisterEvent<AssetId>(EventKey.SelectionChanged, (ctx, state, evt) =>
            {
               state.FileSpecs = EngineController.AssetController.FetchAssetFileSpecs(evt);
            })
            .RegisterEvent<string>(EventKey.SelectionAction, (ctx, state, evt) =>
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
            .OnEnter((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnLeave((ctx, state) => { })
            .Build();
    }

    private ComponentRuntime RegisterVisualState()
    {
        return VisualRuntime = ComponentRuntime
            .CreateBuilder<SlotState<EditorVisualState>, VisualParamComponent>()
            .OnEnter((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave((ctx, state) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, (ctx, state, evt) =>
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