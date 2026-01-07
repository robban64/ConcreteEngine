using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;

namespace ConcreteEngine.Editor.Core;

internal static class EditorModelManager
{
    public static bool HasInit { get; private set; }

    public static ModelStateContext SceneStateContext { get; private set; } = null!;
    public static ModelStateContext AssetStateContext { get; private set; } = null!;
    public static ModelStateContext CameraStateContext { get; private set; } = null!;
    public static ModelStateContext WorldRenderStateContext { get; private set; } = null!;

    private static readonly List<ModelStateContext> RefreshQueue = new (8);

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

        RegisterAssetState();
        RegisterSceneState();
        RegisterCameraState();
        RegisterWorldRenderState();

        SceneStateContext.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterSceneState()
    {
        SceneStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => { })
            .OnLeave(static (_) => { })
            .RegisterEvent(EventKey.CategoryChanged, static () => { })
            .RegisterEvent<ISceneObject>(EventKey.SelectionChanged, static (it) =>
            {
                if (EditorDataStore.SelectedSceneObj == it.Id) return;
                if (it.Id.IsValid())
                    EngineController.SelectEntity(it.Id);
                else
                    EngineController.DeSelectEntity();

                if (!it.Id.IsValid()) return;
                StateContext.SetLeftSidebarState(LeftSidebarMode.Scene);
                StateContext.SetRightSidebarState(RightSidebarMode.Property);
            })
            .RegisterEvent<ISceneObject>(EventKey.SelectionAction, static (_) => { })
            .Build();
    }


    private static void RegisterAssetState()
    {
        AssetStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => AssetsComponent.OnEnter())
            .OnLeave(static (_) => AssetsComponent.ResetState())
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
/*
    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => { })
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

    private static void RegisterCameraState()
    {
        CameraStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => EngineController.FetchCamera())
            .OnRefresh(static (_) => EngineController.FetchCamera())
            .OnLeave(static (_) => { })
            .Build();
    }

    private static void RegisterWorldRenderState()
    {
        WorldRenderStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => EngineController.FetchWorldParams())
            .OnRefresh(static (_) => EngineController.FetchWorldParams())
            .OnLeave(static (_) => { })
            .RegisterEvent<int>(EventKey.WorldActionInvoke, SetShadowSize)
            .Build();
        return;

        static void SetShadowSize(int size)
        {
            var payload = new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(size));
            CommandDispatcher.InvokeEditorCommand(payload);
        }
    }
}