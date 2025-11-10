using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

namespace ConcreteEngine.Editor;

internal static class EditorStateManager
{
    public static ModelState<EntitiesViewModel> EntityModelState { get; }
    public static ModelState<AssetStoreViewModel> AssetModelState { get; }
    public static ModelState<CameraViewModel> CameraModelState { get; }

    private static void SetuoEntityState()
    {
    }

    static EditorStateManager()
    {
        EntityModelState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.SelectionChanged,
                static (ModelState<EntitiesViewModel> ctx, in EntityRecord? ev) => OnFetchEntityData(ev!))
            .RegisterEvent(EventKey.SelectionUpdated,
                static (ModelState<EntitiesViewModel> ctx, in EntityTransformPayload ev) =>
                {
                    StateCtx.ExecuteSetEntityTransform(in ev);
                })
            .Build();

        AssetModelState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => OnFillAssetStore())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.CategoryChanged,
                static (ModelState<AssetStoreViewModel> ctx, in EditorAssetCategory ev) => OnFillAssetStore(ev))
            .RegisterEvent(EventKey.SelectionChanged,
                static (ModelState<AssetStoreViewModel> ctx, in AssetObjectViewModel? ev) => { OnFillAssetFiles(ev); })
            .RegisterEvent(EventKey.SelectionAction,
                static (ModelState<AssetStoreViewModel> ctx, in AssetObjectViewModel ev) =>
                {
                    StateCtx.ExecuteReloadShader(ev);
                })

            .Build();


        CameraModelState = ModelState<CameraViewModel>
            .CreateBuilder(static () => new CameraViewModel())
            .OnEnter(static (ctx, it) =>
            {
                var gen = it.Generation;
                OnFetchCameraData();
                CameraPropertyComponent.UpdateStateFromViewModel(gen > 0);
            })
            .RegisterEvent(EventKey.SelectionUpdated,
                static (ModelState<CameraViewModel> ctx, in CameraEditorPayload ev) =>
                {
                    StateCtx.ExecuteSetCameraTransform(in ev);
                })
            .Build();
    }
}