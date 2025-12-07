#region

using ConcreteEngine.Common;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Core;

internal static class EditorModelManager
{
    public static ModelStateContext<EntitiesViewModel> EntitiesStateContext { get; private set; } = null!;
    public static ModelStateContext<AssetState> AssetStateContext { get; private set; } = null!;
    public static ModelStateContext<CameraState> CameraStateContext { get; private set; } = null!;
    public static ModelStateContext<WorldParamState> WorldRenderStateContext { get; private set; } = null!;

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

        CameraStateContext.InvokeAction(TransitionKey.Enter);
        EntitiesStateContext.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterAssetState()
    {
        AssetStateContext = ModelStateContext<AssetState>
            .CreateBuilder(static () => new AssetState())
            .OnEnter(static (ctx, it) => it.Refresh())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.CategoryChanged, static (ctx) => ctx.State!.Refresh())
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionChanged,
                static (ctx, it) => ctx.State!.GetFileAssets(it, EditorApi.FetchAssetDetailed))
            .RegisterEvent<EditorAssetResource>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;


        static void ReloadShaderHandler(ModelStateContext<AssetState> ctx, EditorAssetResource it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
                new EditorShaderPayload(it.Name, EditorRequestAction.Reload));
    }

    private static void RegisterEntityState()
    {
        EntitiesStateContext = ModelStateContext<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it)=>{})
            .OnRefresh(static (ctx, it)=>{})
            .OnLeave(static (ctx, _) => ctx.TriggerEvent<EditorEntityResource?>(EventKey.SelectionChanged, null))
            .RegisterEvent<EditorEntityResource?>(EventKey.SelectionChanged, OnEntitySelected)
            .RegisterEvent<EditorEntityResource>(EventKey.SelectionUpdated, OnEntityUpdated)
            .KeepAlive()
            .Build();

        return;

        static void OnEntitySelected(ModelStateContext<EntitiesViewModel> ctx, EditorEntityResource? it)
        {
            ctx.State!.SetSelectedEntity(it?.Id ?? EditorId.Empty);
            ctx.EnqueueRefreshNextFrame();
        }

        static void OnEntityUpdated(ModelStateContext<EntitiesViewModel> ctx, EditorEntityResource it)
        {
            ctx.State!.SetSelectedEntity(it.Id);
            ctx.State!.Dispatch(EditorApi.EntityApi, true);
            ctx.EnqueueRefreshNextFrame();
        }
    }

    private static void RegisterCameraState()
    {
        CameraStateContext = ModelStateContext<CameraState>
            .CreateBuilder(static () => new CameraState())
            .OnEnter(FetchCameraDataHandler)
            .OnRefresh(FetchCameraDataHandler)
            .OnLeave(static (ctx, it) => { })
            .RegisterEvent(EventKey.SelectionUpdated, WriteCameraDataHandler)
            .KeepAlive()
            .Build();

        return;

        static void WriteCameraDataHandler(ModelStateContext<CameraState> ctx)
        {
            ctx.State!.Dispatch(EditorApi.CameraApi, true);
            ctx.EnqueueRefreshNextFrame();
        }

        static void FetchCameraDataHandler(ModelStateContext<CameraState> ctx, CameraState it) =>
            it.Dispatch(EditorApi.CameraApi, false);
    }

    private static void RegisterWorldRenderState()
    {
        WorldRenderStateContext = ModelStateContext<WorldParamState>
            .CreateBuilder(static () => new WorldParamState())
            .OnEnter(UpdateData)
            .OnRefresh(UpdateData)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.SelectionChanged, SelectionChangeHandler)
            .RegisterEvent(EventKey.SelectionUpdated, SelectionUpdateHandler)
            .Build();
        return;

        static void UpdateData(ModelStateContext<WorldParamState> ctx, WorldParamState it) =>
            it.Dispatch(EditorApi.WorldParamsApi, false);

        static void SelectionChangeHandler(ModelStateContext<WorldParamState> ctx)
        {
            ctx.State!.Dispatch(EditorApi.WorldParamsApi, false);
        }

        static void SelectionUpdateHandler(ModelStateContext<WorldParamState> ctx)
        {
            ctx.State!.Dispatch(EditorApi.WorldParamsApi, true);
            //ctx.EnqueueRefreshNextFrame();
        }
    }
}