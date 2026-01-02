using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Editor.Core;

internal static class EditorModelManager
{
    public static ModelStateContext SceneStateContext { get; private set; } = null!;
    public static ModelStateContext EntitiesStateContext { get; private set; } = null!;
    public static ModelStateContext AssetStateContext { get; private set; } = null!;
    public static ModelStateContext CameraStateContext { get; private set; } = null!;
    public static ModelStateContext WorldRenderStateContext { get; private set; } = null!;

    public static bool HasInit { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InvokeRefreshForModels()
    {
        SceneStateContext.TryInvokePendingRefresh();
        EntitiesStateContext.TryInvokePendingRefresh();
        AssetStateContext.TryInvokePendingRefresh();
        CameraStateContext.TryInvokePendingRefresh();
        WorldRenderStateContext.TryInvokePendingRefresh();
    }

    public static void Initialize()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntitiesStateContext));

        HasInit = true;

        RegisterAssetState();
        RegisterSceneState();
        RegisterEntityState();
        RegisterCameraState();
        RegisterWorldRenderState();

        EntitiesStateContext.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterSceneState()
    {
        SceneStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => { })
            .OnLeave(static (_) => { })
            .RegisterEvent(EventKey.CategoryChanged, static () => { })
            .RegisterEvent<EditorSceneObject>(EventKey.SelectionChanged, static (it) =>
            {
                var id = it?.Id ?? EditorId.Empty;
                if (EditorDataStore.SelectedSceneObject == id) return;
                if (!id.IsValid) return;
                EditorDataStore.SelectedSceneObject = id;
                StateContext.SetLeftSidebarState(LeftSidebarMode.Scene);
                StateContext.SetRightSidebarState(RightSidebarMode.SceneObject);
            })
            .RegisterEvent<EditorSceneObject>(EventKey.SelectionAction, static (_) => { })
            .Build();
    }


    private static void RegisterAssetState()
    {
        AssetStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => { })
            .OnLeave(static (_) => AssetsComponent.ResetState())
            .RegisterEvent(EventKey.CategoryChanged, static () => { })
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionChanged, FetchAssetDetailed)
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;

        static void FetchAssetDetailed(EditorAssetResource it) =>
            AssetsComponent.FileAssets = EngineController.AssetController.GetAssetFiles(it.Id);

        static void ReloadShaderHandler(EditorAssetResource it)
        {
            var cmd = new AssetCommandRecord(CommandAssetAction.Reload, AssetKind.Shader, it.Name);
            CommandDispatcher.InvokeEditorCommand(cmd);
        }
    }

    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (_) => { })
            .OnRefresh(static (_) => { })
            .OnLeave(static (ctx) => ctx.TriggerEvent<EditorEntityResource?>(EventKey.SelectionChanged, null))
            .RegisterEvent<EditorEntityResource?>(EventKey.SelectionChanged, OnEntitySelected)
            .Build();

        return;

        static void OnEntitySelected(EditorEntityResource? it)
        {
            var entity = it?.Id ?? EditorId.Empty;
            if (EditorDataStore.SelectedEntity == entity) return;
            if (entity.IsValid)
                EngineController.SelectEntity(entity);
            else
                EngineController.DeSelectEntity();

            if (!entity.IsValid) return;
            StateContext.SetLeftSidebarState(LeftSidebarMode.Entities);
            StateContext.SetRightSidebarState(RightSidebarMode.Property);
        }
    }

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