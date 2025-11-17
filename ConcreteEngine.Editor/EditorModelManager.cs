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

        ModelManager.CameraState.InvokeAction(TransitionKey.Enter);
        ModelManager.EntitiesState.InvokeAction(TransitionKey.Enter);
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
            .OnEnter(static (ctx, it) => it.FillView(EditorApi.FetchEntityView))
            .OnRefresh(RefreshEntityState)
            .OnLeave(LeaveEntityState)
            .RegisterEvent<EntityRecord?>(EventKey.SelectionChanged, SelectEntityHandler)
            .RegisterEvent<EntityRecord>(EventKey.SelectionUpdated, UpdateEntityHandler)
            .RegisterEvent<EditorMouseSelectPayload>(EventKey.MouseClick, ClickHandler)
            .KeepAlive()
            .Build();

        return;
        

        static void ClickHandler(ModelState<EntitiesViewModel> ctx, EditorMouseSelectPayload payload)
        {
            EditorApi.SendClickRequest(in payload, out var response);
            if (response.EntityId == 0) return;

            var entity = response.EntityId == ctx.State!.Data.EntityId ? null : ctx.State.GetEntity(response.EntityId);
            ctx.TriggerEvent(EventKey.SelectionChanged, entity);
        }

        static void LeaveEntityState(ModelState<EntitiesViewModel> ctx, EntitiesViewModel it)
            => it.FillData(null, in EditorApi.UpdateEntityData);

        static void RefreshEntityState(ModelState<EntitiesViewModel> ctx, EntitiesViewModel it)
        {
            ctx.State!.FillData(in EditorApi.UpdateEntityData);
        }

        static void SelectEntityHandler(ModelState<EntitiesViewModel> ctx, EntityRecord? it)
        {
            if (ctx.State!.Entities.Count == 0)
                ctx.State!.FillView( EditorApi.FetchEntityView);

            ctx.State!.FillData(it, in EditorApi.UpdateEntityData);
        }

        static void UpdateEntityHandler(ModelState<EntitiesViewModel> ctx, EntityRecord it)
        {
            ctx.State!.WriteData(it, in EditorApi.UpdateEntityData);
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
            ctx.State!.WriteData(in EditorApi.UpdateCameraData);
            ctx.EnqueueRefreshNextFrame();
        }

        static void FetchCameraDataHandler(ModelState<CameraViewModel> ctx, CameraViewModel it) =>
            it.FillData(EditorApi.UpdateCameraData);
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
            it.FillData(in EditorApi.UpdateWorldParams);

        static void SelectionChangeHandler(ModelState<WorldRenderViewModel> ctx, WorldParamSelection it)
        {
            ctx.State!.Selection = it;
            ctx.State!.FillData(in EditorApi.UpdateWorldParams);
        }

        static void SelectionUpdateHandler(ModelState<WorldRenderViewModel> ctx)
        {
            ctx.State!.WriteData(in EditorApi.UpdateWorldParams);
            ctx.EnqueueRefreshNextFrame();
        }
    }
}