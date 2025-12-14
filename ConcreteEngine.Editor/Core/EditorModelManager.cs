using ConcreteEngine.Common;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Core;

internal static class EditorModelManager
{
    public static ModelStateContext EntitiesStateContext { get; private set; } = null!;
    public static ModelStateContext AssetStateContext { get; private set; } = null!;
    public static ModelStateContext CameraStateContext { get; private set; } = null!;
    public static ModelStateContext WorldRenderStateContext { get; private set; } = null!;

    public static bool HasInit { get; private set; } = false;

    internal static void InvokeRefreshForModels()
    {
        EntitiesStateContext.TryInvokePendingRefresh();
        AssetStateContext.TryInvokePendingRefresh();
        CameraStateContext.TryInvokePendingRefresh();
        WorldRenderStateContext.TryInvokePendingRefresh();
    }

    public static void Initialize()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntitiesStateContext));

        HasInit = true;

        RegisterEntityState();
        RegisterAssetState();
        RegisterCameraState();
        RegisterWorldRenderState();
        RegisterWorldObjectState();

        EntitiesStateContext.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterWorldObjectState()
    {
        /*
        WorldObjectStateContext = ModelStateContext<ObjectComponentState>
            .CreateBuilder(static () => new ObjectComponentState())
            .OnEnter(static (ctx, it) => { })
            .OnLeave(static (ctx, it) =>
            {
                EditorDataStore.Slot<ParticleDataState>.Data.EmitterHandle = 0;
                ctx.ResetState();
            })
            .RegisterEvent(EventKey.CategoryChanged, static (ctx) =>
            {
                var state = ctx.State!;
                state.Particles = [];
                state.Animations = [];
                if (state.Selection == WorldObjectSelection.Particle)
                    state.Particles = EditorApi.LoadParticleResources();
                else if (state.Selection == WorldObjectSelection.Animation)
                    state.Animations = EditorApi.LoadAnimationResources();
            })
            .Build();
            */
    }


    private static void RegisterAssetState()
    {
        AssetStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (ctx) => { })
            .OnLeave(static (ctx) => AssetsComponent.ResetState())
            .RegisterEvent(EventKey.CategoryChanged, static () => { })
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionChanged, FetchAssetDetailed)
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;

        static void FetchAssetDetailed(EditorAssetResource it) =>
            AssetsComponent.FileAssets = EditorApi.FetchAssetDetailed(new EditorFetchHeader(it.Id));

        static void ReloadShaderHandler(EditorAssetResource it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
                new EditorShaderCommand(it.Name, EditorRequestAction.Reload));
    }

    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (ctx) => { })
            .OnRefresh(static (ctx) => { })
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
            .OnEnter(static (ctx) => EngineController.FetchCamera())
            .OnRefresh(static (ctx) => EngineController.FetchCamera())
            .OnLeave(static (ctx) => { })
            .Build();
    }

    private static void RegisterWorldRenderState()
    {
        WorldRenderStateContext = ModelStateContext
            .CreateBuilder()
            .OnEnter(static (ctx) => EngineController.FetchWorldParams())
            .OnRefresh(static (ctx) => EngineController.FetchWorldParams())
            .OnLeave(static (ctx) => { })
            .RegisterEvent<EditorShadowCommand>(EventKey.WorldActionInvoke, SetShadowSize)
            .Build();
        return;

        static void SetShadowSize(EditorShadowCommand evt) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.WorldShadow, evt);
    }
}