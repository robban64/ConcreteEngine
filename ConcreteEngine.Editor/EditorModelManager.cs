#region

using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

#endregion

namespace ConcreteEngine.Editor;

internal static class EditorModelManager
{
    public static ModelState<EntitiesViewModel> EntitiesState { get; private set; } = null!;
    public static ModelState<AssetStoreViewModel> AssetState { get; private set; } = null!;
    public static ModelState<CameraViewModel> CameraState { get; private set; } = null!;
    public static ModelState<WorldRenderViewModel> WorldRenderState { get; private set; } = null!;

    public static bool HasInit { get; private set; } = false;

    internal static void InvokeRefreshForModels()
    {
        EntitiesState.TryInvokePendingRefresh();
        AssetState.TryInvokePendingRefresh();
        CameraState.TryInvokePendingRefresh();
        WorldRenderState.TryInvokePendingRefresh();
    }

    public static void SetupModelState()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntitiesState));

        HasInit = true;
        RegisterEntityState();
        RegisterAssetState();
        RegisterCameraState();
        RegisterWorldRenderState();

        CameraState.InvokeAction(TransitionKey.Enter);
        EntitiesState.InvokeAction(TransitionKey.Enter);
    }

    private static void RegisterAssetState()
    {
        AssetState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => ctx.State!.FillView(it.Category, EditorApi.FetchAssetStoreData))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EditorAssetCategory>(EventKey.CategoryChanged,
                static (ctx, it) => ctx.State!.FillView(it, EditorApi.FetchAssetStoreData))
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionChanged,
                static (ctx, it) => ctx.State!.FillAssetFileView(it, EditorApi.FetchAssetObjectFiles))
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;


        static void ReloadShaderHandler(ModelState<AssetStoreViewModel> ctx, AssetObjectViewModel it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
                new EditorShaderPayload(it.Name, EditorRequestAction.Reload));
    }

    private static void RegisterEntityState()
    {
        EntitiesState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => FillEntityData(ctx, it.GetSelectedEntity()))
            .OnRefresh(static (ctx, it) => FillEntityData(ctx, it.GetSelectedEntity()))
            .OnLeave(static (ctx, _) => FillEntityData(ctx, null))
            .RegisterEvent<EntityRecord?>(EventKey.SelectionChanged, FillEntityData)
            .RegisterEvent<EntityRecord>(EventKey.SelectionUpdated, UpdateEntityHandler)
            .RegisterEvent<EditorMouseSelectPayload>(EventKey.MouseClick, ClickHandler)
            .KeepAlive()
            .Build();

        return;

        static void FillEntityData(ModelState<EntitiesViewModel> ctx, EntityRecord? it)
        {
            var model = ctx.State!;
            if (model.Entities.Count == 0)
            {
                model.ClearDataState();
                model.FillView(EditorApi.FetchEntityView);
                return;
            }

            model.FillData(it, in EditorApi.FillEntityData);
        }

        static void ClickHandler(ModelState<EntitiesViewModel> ctx, EditorMouseSelectPayload payload)
        {
            EditorApi.SendClickRequest(in payload, out var response);
            var state = ctx.State!;

            if (response.EntityId == 0)
            {
                if(state.Data.EntityId > 0)
                    state.FillData(null, in EditorApi.FillEntityData);
                
                state.ClearDataState();
            }
            else if (response.EntityId == state.Data.EntityId)
            {
                state.FillData(state.GetSelectedEntity(), in EditorApi.FillEntityData);
            }
            else
            {
                if(state.Entities.Count == 0)
                    state.FillView(EditorApi.FetchEntityView);
                
                ctx.TriggerEvent(EventKey.SelectionChanged, state.GetEntity(response.EntityId));
            }

        }

        static void UpdateEntityHandler(ModelState<EntitiesViewModel> ctx, EntityRecord it)
        {
            ctx.State!.WriteData(it, in EditorApi.FillEntityData);
            ctx.EnqueueRefreshNextFrame();
        }
    }

    private static void RegisterCameraState()
    {
        CameraState = ModelState<CameraViewModel>
            .CreateBuilder(static () => new CameraViewModel())
            .OnEnter(FetchCameraDataHandler)
            .OnRefresh(FetchCameraDataHandler)
            .OnLeave(static (ctx, it) => { })
            .RegisterEmptyEvent(EventKey.SelectionUpdated, WriteCameraDataHandler)
            .KeepAlive()
            .Build();

        return;

        static void WriteCameraDataHandler(ModelState<CameraViewModel> ctx)
        {
            ctx.State!.WriteData(in EditorApi.FillCameraData);
            ctx.EnqueueRefreshNextFrame();
        }

        static void FetchCameraDataHandler(ModelState<CameraViewModel> ctx, CameraViewModel it) =>
            it.FillData(EditorApi.FillCameraData);
    }

    private static void RegisterWorldRenderState()
    {
        WorldRenderState = ModelState<WorldRenderViewModel>
            .CreateBuilder(static () => new WorldRenderViewModel())
            .OnEnter(UpdateData)
            .OnRefresh(UpdateData)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<WorldParamSelection>(EventKey.SelectionChanged, SelectionChangeHandler)
            .RegisterEmptyEvent(EventKey.SelectionUpdated, SelectionUpdateHandler)
            .Build();
        return;

        static void UpdateData(ModelState<WorldRenderViewModel> ctx, WorldRenderViewModel it) =>
            it.FillData(in EditorApi.FillWorldParams);

        static void SelectionChangeHandler(ModelState<WorldRenderViewModel> ctx, WorldParamSelection it)
        {
            ctx.State!.Selection = it;
            ctx.State!.FillData(in EditorApi.FillWorldParams);
        }

        static void SelectionUpdateHandler(ModelState<WorldRenderViewModel> ctx)
        {
            ctx.State!.WriteData(in EditorApi.FillWorldParams);
            ctx.EnqueueRefreshNextFrame();
        }
    }
}