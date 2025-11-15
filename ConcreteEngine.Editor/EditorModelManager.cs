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

    static EditorModelManager()
    {
    }

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
    }

    private static void RegisterAssetState()
    {
        AssetState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => OnFillAssetStore(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EditorAssetCategory>(EventKey.CategoryChanged, SelectAssetHandler)
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionChanged, FillAssetFilesHandler)
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionAction, ReloadShaderHandler)
            .Build();
        return;

        static void SelectAssetHandler(ModelState<AssetStoreViewModel> ctx, in EditorAssetCategory it) =>
            OnFillAssetStore(ctx, it);

        static void FillAssetFilesHandler(ModelState<AssetStoreViewModel> ctx, in AssetObjectViewModel? it) =>
            OnFillAssetFiles(ctx, it);

        static void ReloadShaderHandler(ModelState<AssetStoreViewModel> ctx, in AssetObjectViewModel it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.AssetShader,
                new EditorShaderPayload(it.Name, EditorRequestAction.Reload));
    }

    private static void RegisterEntityState()
    {
        EntitiesState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities(ctx))
            .OnRefresh(RefreshEntity)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EntityRecord>(EventKey.SelectionChanged, SelectEntityHandler)
            .RegisterEvent<EntityRecord>(EventKey.SelectionUpdated,
                UpdateEntityHandler)
            .Build();
        return;

        static void RefreshEntity(ModelState<EntitiesViewModel> ctx, EntitiesViewModel it) =>
            ctx.State!.FillData(in EditorApi.UpdateEntityData);

        static void SelectEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityRecord it) =>
            ctx.State!.FillData(it, in EditorApi.UpdateEntityData);

        static void UpdateEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityRecord it)
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
            .RegisterEventNoOp(EventKey.SelectionUpdated, WriteCameraDataHandler)
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
            .RegisterEventNoOp(EventKey.SelectionUpdated, SelectionUpdateHandler)
            .Build();
        return;

        static void UpdateData(ModelState<WorldRenderViewModel> ctx, WorldRenderViewModel it) =>
            it.FillData(in EditorApi.UpdateWorldParams);

        static void SelectionChangeHandler(ModelState<WorldRenderViewModel> ctx, in WorldParamSelection it)
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