using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Metrics;

namespace ConcreteEngine.Editor.Core;

internal sealed class EmptyState
{
    public static EmptyState Instance { get; } = new();
}

internal static class ModelManager
{
    public static bool HasInit { get; private set; }

    public static ModelStateContext MetricsStateContext { get; private set; } = null!;
    public static ModelStateContext SceneStateContext { get; private set; } = null!;
    public static ModelStateContext AssetStateContext { get; private set; } = null!;
    public static ModelStateContext CameraStateContext { get; private set; } = null!;
    public static ModelStateContext VisualStateContext { get; private set; } = null!;

    private static readonly List<ModelStateContext> RefreshQueue = new(8);

    public static ReadOnlySpan<ModelStateContext> GetRefreshQueue() => CollectionsMarshal.AsSpan(RefreshQueue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InvokeRefreshForModels()
    {
        foreach (var it in RefreshQueue)
            it.TryInvokePendingRefresh();
    }

    public static void EnqueueRefresh(ModelStateContext modelState) => RefreshQueue.Add(modelState);

    public static void Initialize()
    {
        HasInit = true;
        RegisterMetrics();
        RegisterSceneState();
        RegisterAssetState();
        RegisterCameraState();
        RegisterVisualState();

        SceneStateContext.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterMetrics()
    {
        MetricsStateContext = ModelStateContext.CreateBuilder<EmptyState>(ComponentDrawKind.Both)
            .MakeState(
                static (_) => MetricsComponent.DrawMetricLeft(),
                static (_) => MetricsComponent.DrawMetricRight()
            )
            .OnEnter(static (state, ctx) => MetricsApi.EnterMetricMode())
            .OnLeave(static (state, ctx) => MetricsApi.LeaveMetricMode())
            .Build();
    }


    private static void RegisterSceneState()
    {
        SceneStateContext = ModelStateContext.CreateBuilder<EmptyState>(ComponentDrawKind.Both)
            .MakeState(SceneListComponent.Draw, ScenePropertyComponent.Draw)
            .OnEnter(static (state, ctx) => { })
            .OnLeave(static (state, ctx) => { })
            .RegisterEvent<SceneObjectId>(EventKey.SelectionChanged, static (id) =>
            {
                if (StoreHub.SelectedId.Id == id) return;
                if (id.IsValid()) EngineController.SelectSceneObject(id);
                else EngineController.DeSelectSceneObject();
                StateManager.SetLeftSidebarState(LeftSidebarMode.Scene);
                StateManager.SetRightSidebarState(RightSidebarMode.Property);
            })
            .Build();
    }


    private static void RegisterAssetState()
    {
        AssetStateContext = ModelStateContext.CreateBuilder<EmptyState>(ComponentDrawKind.Left)
            .MakeState(static (_) => AssetsComponent.Draw())
            .OnEnter(static (state, ctx) => AssetsComponent.OnEnter())
            .OnLeave(static (state, ctx) => AssetsComponent.ResetState())
            .RegisterEvent<AssetObject>(EventKey.SelectionChanged, FetchAssetDetailed)
            .RegisterEvent<AssetObject>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;

        static void FetchAssetDetailed(AssetObject asset) =>
            AssetsComponent.FileSpecs = EngineController.AssetController.FetchAssetFileSpecs(asset.Id);

        static void ReloadShaderHandler(AssetObject asset)
        {
            var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, asset.Name);
            CommandDispatcher.InvokeEditorCommand(cmd);
        }
    }


    private static void RegisterCameraState()
    {
        CameraStateContext = ModelStateContext.CreateBuilder<SlotState<EditorCameraState>>(ComponentDrawKind.Right)
            .MakeState(CameraComponent.Draw)
            .OnEnter((state, ctx) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnRefresh((state, ctx) => EngineController.WorldController.FetchCamera(state.GetView()))
            .OnLeave( (state, ctx) => {})
            .Build();
        
        
    }

    private static void RegisterVisualState()
    {
        VisualStateContext = ModelStateContext.CreateBuilder<SlotState<EditorVisualState>>(ComponentDrawKind.Right)
            .MakeState( VisualParamComponent.Draw)
            .OnEnter((state, ctx) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnRefresh((state, ctx) => EngineController.WorldController.FetchVisualParams(state.GetView()))
            .OnLeave( (state, ctx) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, SetShadowSize)
            .Build();
        return;

        static void SetShadowSize(int size)
        {
            var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(size));
            CommandDispatcher.InvokeEditorCommand(payload);
        }
    }
/*
    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (state, ctx) => { })
            .OnRefresh(static (_) => { })
            .OnLeave(static (ctx) => ctx.TriggerEvent<ISceneObject?>(EventKey.SelectionChanged, null))
            .RegisterEvent<ISceneObject?>(EventKey.SelectionChanged, OnEntitySelected)
            .Build();

        return;

        static void OnEntitySelected(ISceneObject? it)
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