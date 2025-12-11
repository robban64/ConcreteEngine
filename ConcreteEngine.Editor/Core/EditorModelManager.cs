#region

using ConcreteEngine.Common;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorModelManager
{
    public static ModelStateContext<EntitiesViewModel> EntitiesStateContext { get; private set; } = null!;
    public static ModelStateContext<AssetState> AssetStateContext { get; private set; } = null!;
    public static ModelStateContext<CameraState> CameraStateContext { get; private set; } = null!;
    public static ModelStateContext<WorldParamState> WorldRenderStateContext { get; private set; } = null!;
    public static ModelStateContext<ObjectComponentState> WorldObjectStateContext { get; private set; } = null!;

    public static bool HasInit { get; private set; } = false;

    internal static void InvokeRefreshForModels()
    {
        EntitiesStateContext.TryInvokePendingRefresh();
        AssetStateContext.TryInvokePendingRefresh();
        CameraStateContext.TryInvokePendingRefresh();
        WorldRenderStateContext.TryInvokePendingRefresh();
    }

    public static void SetupModelState()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntitiesStateContext));

        HasInit = true;

        RegisterEntityState();
        RegisterAssetState();
        RegisterCameraState();
        RegisterWorldRenderState();
        RegisterWorldObjectState();

        CameraStateContext.InvokeAction(TransitionKey.Enter);
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
        AssetStateContext = ModelStateContext<AssetState>
            .CreateBuilder(static () => new AssetState())
            .OnEnter(static (ctx, it) => { })
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.CategoryChanged, static (ctx) => { })
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionChanged, FetchAssetDetailed)
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;

        static void FetchAssetDetailed(ModelStateContext<AssetState> ctx, EditorAssetResource it) =>
            ctx.State!.SetFileAssets(EditorApi.FetchAssetDetailed(new EditorFetchHeader(it.Id)));

        static void ReloadShaderHandler(ModelStateContext<AssetState> ctx, EditorAssetResource it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
                new EditorShaderCommand(it.Name, EditorRequestAction.Reload));
    }

    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => { })
            .OnRefresh(static (ctx, it) => { })
            .OnLeave(static (ctx, _) => ctx.TriggerEvent<EditorEntityResource?>(EventKey.SelectionChanged, null))
            .RegisterEvent<EditorEntityResource?>(EventKey.SelectionChanged, OnEntitySelected)
            .RegisterEvent<EditorEntityResource>(EventKey.SelectionUpdated, OnEntityUpdated)
            .KeepAlive()
            .Build();

        return;

        static void OnEntitySelected(ModelStateContext<EntitiesViewModel> ctx, EditorEntityResource? it)
        {
            //ctx.State!.SetSelectedEntity(it?.Id ?? EditorId.Empty);
            //ctx.EnqueueRefreshNextFrame();
        }

        static void OnEntityUpdated(ModelStateContext<EntitiesViewModel> ctx, EditorEntityResource it)
        {
            /*
            ctx.State!.SetSelectedEntity(it.Id);
            ctx.State!.Dispatch(EditorApi.EntityApi, true);
            ctx.EnqueueRefreshNextFrame();
            */
        }
    }

    private static void RegisterCameraState()
    {
        CameraStateContext = ModelStateContext<CameraState>
            .CreateBuilder(static () => new CameraState())
            .OnEnter(static (ctx, it) => EditorDataStore.Slot<CameraDataState>.SlotState.RequestData())
            .OnRefresh(static (ctx, it) => { })
            .OnLeave(static (ctx, it) => { })
            .RegisterEvent(EventKey.SelectionUpdated, static (ctx) => { })
            .KeepAlive()
            .Build();
    }

    private static void RegisterWorldRenderState()
    {
        WorldRenderStateContext = ModelStateContext<WorldParamState>
            .CreateBuilder(static () => new WorldParamState())
            .OnEnter(static (ctx, it) => EditorDataStore.Slot<WorldParamsData>.SlotState.RequestData())
            .OnRefresh(static (ctx, it) => EditorDataStore.Slot<WorldParamsData>.SlotState.RequestData())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EditorShadowCommand>(EventKey.WorldActionInvoke, SetShadowSize)
            .Build();
        return;

        static void SetShadowSize(ModelStateContext<WorldParamState> ctx, EditorShadowCommand evt) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.WorldShadow, evt);
    }
}