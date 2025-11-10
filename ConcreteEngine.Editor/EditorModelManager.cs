using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

namespace ConcreteEngine.Editor;

internal static class EditorModelManager
{
    public static ModelState<EntitiesViewModel> EntityModelState { get; private set; } = null!;
    public static ModelState<AssetStoreViewModel> AssetModelState { get; private set; } = null!;
    public static ModelState<CameraViewModel> CameraModelState { get; private set; } = null!;

    public static bool HasInit { get; private set; } = false;

    static EditorModelManager()
    {
    }

    public static void SetupModelState()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntityModelState));

        HasInit = true;
        RegisterEntityState();
        RegisterAssetState();
        RegisterCameraState();
    }

    private static void RegisterEntityState()
    {
        EntityModelState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EntityRecord>(EventKey.SelectionChanged, SelectEntityHandler)
            .RegisterEvent<EntityTransformPayload>(EventKey.SelectionUpdated,
                UpdateEntityHandler)
            .Build();
        return;

        static void SelectEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityRecord it) =>
            ctx.State!.UpdateDataFrom(it, EditorApi.UpdateEntityData!);

        static void UpdateEntityHandler(ModelState<EntitiesViewModel> ctx, in EntityTransformPayload it) =>
            CommandDispatcher.InvokeEditorCommand(CoreCmdNames.EntityTransform, in it);
    }


    private static void RegisterAssetState()
    {
        AssetModelState = ModelState<AssetStoreViewModel>
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
    
    private static void RegisterCameraState()
    {
        CameraModelState = ModelState<CameraViewModel>
            .CreateBuilder(static () => new CameraViewModel())
            .OnEnter(FetchCameraDataHandler)
            .OnRefresh(FetchCameraDataHandler)
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<CameraEditorPayload>(EventKey.SelectionUpdated, Handler)
            .Build();

        return;

        static void FetchCameraDataHandler(ModelState<CameraViewModel> ctx, CameraViewModel it) =>
            it.UpdateState(EditorApi.UpdateCameraData!);

        static void Handler(ModelState<CameraViewModel> ctx, in CameraEditorPayload it)
            => CommandDispatcher.InvokeEditorCommand(CoreCmdNames.CameraTransform, in it);
    }
}