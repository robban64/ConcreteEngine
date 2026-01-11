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

    /*
    private  readonly List<ModelStateObject> RefreshQueue = new(8);

    public  ReadOnlySpan<ModelStateObject> GetRefreshQueue() => CollectionsMarshal.AsSpan(RefreshQueue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal  void InvokeRefreshForModels()
    {
        foreach (var it in RefreshQueue)
            it.TryInvokePendingRefresh();
    }

    public  void EnqueueRefresh(ModelStateObject modelState) => RefreshQueue.Add(modelState);
*/

    public void Initialize(GlobalContext ctx)
    {
        HasInit = true;
        ModelStateComponent.TempGlobalContext = ctx;
        RegisterMetrics();
        RegisterSceneState();
        RegisterAssetState();
        RegisterCameraState();
        RegisterVisualState();
        ModelStateComponent.TempGlobalContext = null;

        //SceneStateComponent.InvokeAction(TransitionKey.Enter);
    }

    private void RegisterMetrics()
    {
        MetricsStateComponent = ModelStateComponent
            .CreateBuilder<EmptyState, MetricsComponent>(ComponentDrawKind.Both)
            .OnEnter((ctx, state) => MetricsApi.EnterMetricMode())
            .OnLeave((ctx, state) => MetricsApi.LeaveMetricMode())
            .Build();
    }


    private void RegisterSceneState()
    {
        SceneStateComponent = ModelStateComponent
            .CreateBuilder<SceneState, SceneComponent>(ComponentDrawKind.Both)
            .OnEnter((ctx, state) => { })
            .OnLeave((ctx, state) => { })
            .RegisterEvent<SceneObjectId>(EventKey.SelectionChanged, (ctx, id) =>
            {
                if (StoreHub.SelectedId.Id == id) return;
                if (id.IsValid()) EngineController.SelectSceneObject(id);
                else EngineController.DeSelectSceneObject();
                ctx.EditorState.SetLeftSidebarState(LeftSidebarMode.Scene);
                ctx.EditorState.SetRightSidebarState(RightSidebarMode.Property);
            })
            .Build();
    }


    private void RegisterAssetState()
    {
        AssetStateComponent = ModelStateComponent
            .CreateBuilder<AssetState, AssetsComponent>(ComponentDrawKind.Left)
            .OnEnter((ctx, state) => {})
            .OnLeave((ctx, state) => state.ResetState())
            .RegisterEvent<AssetObject>(EventKey.SelectionChanged, (state, it) =>
            {
               // state.FileSpecs = EngineController.AssetController.FetchAssetFileSpecs(it.Id);
            })
            .RegisterEvent<AssetObject>(EventKey.SelectionAction, (state, it) =>
            {
                var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, it.Name);
                CommandDispatcher.InvokeEditorCommand(cmd);
            })
            .Build();
    }


    private void RegisterCameraState()
    {
        CameraStateComponent = ModelStateComponent
            .CreateBuilder<SlotState<EditorCameraState>, CameraComponent>(ComponentDrawKind.Right)
            .OnEnter((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnLeave((ctx, state) => { })
            .Build();
    }

    private void RegisterVisualState()
    {
        VisualStateComponent = ModelStateComponent
            .CreateBuilder<SlotState<EditorVisualState>, VisualParamComponent>(ComponentDrawKind.Right)
            .OnEnter((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnRefresh((ctx, state) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave((ctx, state) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, (state, size) =>
            {
                var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(size));
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