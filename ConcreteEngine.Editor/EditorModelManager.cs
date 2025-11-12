using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

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

    private static unsafe void RegisterEntityState()
    {
        EntitiesState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EntityRecord>(EventKey.SelectionChanged, &SelectEntityHandler)
            .RegisterEvent<EntityTransformPayload>(EventKey.SelectionUpdated,
                &UpdateEntityHandler)
            .Build();
        return;

        static unsafe void SelectEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityRecord it) =>
            ctx.State!.UpdateDataFrom(it, EditorApi.UpdateEntityData);

        static void UpdateEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityTransformPayload it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.EntityTransform, in it);
    }


    private static unsafe void RegisterAssetState()
    {
        AssetState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => OnFillAssetStore(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EditorAssetCategory>(EventKey.CategoryChanged, &SelectAssetHandler)
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionChanged, &FillAssetFilesHandler)
            .RegisterEvent<AssetObjectViewModel>(EventKey.SelectionAction, &ReloadShaderHandler)
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

    private static unsafe void RegisterCameraState()
    {
        CameraState = ModelState<CameraViewModel>
            .CreateBuilder(static () => new CameraViewModel())
            .OnEnter(FetchCameraDataHandler)
            .OnRefresh(FetchCameraDataHandler)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<CameraEditorPayload>(EventKey.SelectionUpdated, &Handler)
            .Build();

        return;

        static void FetchCameraDataHandler(ModelState<CameraViewModel> ctx, CameraViewModel it) =>
            it.UpdateState(EditorApi.UpdateCameraData);

        static void Handler(ModelState<CameraViewModel> ctx, in CameraEditorPayload it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.CameraTransform, in it);
    }

    private static unsafe void RegisterWorldRenderState()
    {
        WorldRenderState = ModelState<WorldRenderViewModel>
            .CreateBuilder(static () => new WorldRenderViewModel())
            .OnEnter(UpdateData)
            .OnRefresh(UpdateData)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<WorldParamSelection>(EventKey.SelectionChanged, &SelectionChangeHandler)
            .RegisterEventNoOp(EventKey.SelectionUpdated, &SelectionUpdateHandler)
            .Build();
        return;

        static void UpdateData(ModelState<WorldRenderViewModel> ctx, WorldRenderViewModel it) =>
            it.WriteTo(in EditorApi.UpdateWorldParams);

        static void SelectionChangeHandler(ModelState<WorldRenderViewModel> ctx, in WorldParamSelection it)
        {
            ctx.State!.Selection = it;
            ctx.State!.WriteTo(in EditorApi.UpdateWorldParams);
        }

        static void SelectionUpdateHandler(ModelState<WorldRenderViewModel> ctx, in NoOpEvent it)
        {
            ctx.State!.WriteFrom(in EditorApi.UpdateWorldParams);
            ctx.EnqueueRefreshNextFrame();
        }
    }
}