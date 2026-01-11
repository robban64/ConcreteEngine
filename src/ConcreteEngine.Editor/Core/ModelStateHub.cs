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

    public ModelStateComponent MetricsStateComponent { get; private set; } = null!;
    public ModelStateComponent SceneStateComponent { get; private set; } = null!;
    public ModelStateComponent AssetStateComponent { get; private set; } = null!;
    public ModelStateComponent CameraStateComponent { get; private set; } = null!;
    public ModelStateComponent VisualStateComponent { get; private set; } = null!;

    private readonly List<ModelStateComponent> _stateComponents = new (8);

    public void Initialize(GlobalContext ctx)
    {
        HasInit = true;
        _stateComponents.Add(RegisterMetrics());
        _stateComponents.Add(RegisterSceneState());
        _stateComponents.Add(RegisterAssetState());
        _stateComponents.Add(RegisterCameraState());
        _stateComponents.Add(RegisterVisualState());

        //SceneStateComponent.InvokeAction(TransitionKey.Enter);
    }

    private ModelStateComponent RegisterMetrics()
    {
        return MetricsStateComponent = ModelStateComponent
            .CreateBuilder<EmptyState, MetricsComponent>(ComponentDrawKind.Both)
            .OnEnter((ctx, state) => MetricsApi.EnterMetricMode())
            .OnLeave((ctx, state) => MetricsApi.LeaveMetricMode())
            .Build();
    }


    private ModelStateComponent RegisterSceneState()
    {
        return SceneStateComponent = ModelStateComponent
            .CreateBuilder<SceneState, SceneComponent>(ComponentDrawKind.Both)
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


    private ModelStateComponent RegisterAssetState()
    {
        return AssetStateComponent = ModelStateComponent
            .CreateBuilder<AssetState, AssetsComponent>(ComponentDrawKind.Left)
            .OnEnter((ctx, state) => {})
            .OnLeave((ctx, state) => state.ResetState())
            .RegisterEvent<AssetObject>(EventKey.SelectionChanged, (ctx, evt) =>
            {
                var state = evt.GetState<AssetState>();
               state.FileSpecs = EngineController.AssetController.FetchAssetFileSpecs(evt.Msg.Id);
            })
            .RegisterEvent<AssetObject>(EventKey.SelectionAction, (ctx, evt) =>
            {
                var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, evt.Msg.Name);
                CommandDispatcher.InvokeEditorCommand(cmd);
            })
            .Build();
    }


    private ModelStateComponent RegisterCameraState()
    {
        return CameraStateComponent = ModelStateComponent
            .CreateBuilder<SlotState<EditorCameraState>, CameraComponent>(ComponentDrawKind.Right)
            .OnEnter((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnLeave((ctx, state) => { })
            .Build();
    }

    private ModelStateComponent RegisterVisualState()
    {
        return VisualStateComponent = ModelStateComponent
            .CreateBuilder<SlotState<EditorVisualState>, VisualParamComponent>(ComponentDrawKind.Right)
            .OnEnter((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave((ctx, state) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, (ctx, evt) =>
            {
                var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(evt.Msg));
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