using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

namespace ConcreteEngine.Editor;

internal static class EditorStateManager
{
    public static ModelState<EntityListViewModel> EntityModelState { get; }
    public static ModelState<AssetStoreViewModel> AssetModelState { get; }
    public static ModelState<CameraViewModel> CameraModelState { get; }

    static EditorStateManager()
    {
        EntityModelState = ModelState<EntityListViewModel>
            .CreateBuilder(static () => new EntityListViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EntityViewModel?>("SelectionChanged", static (ctx, state, ev) => OnFetchEntityData(ev!))
            .Build();

        AssetModelState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => OnFillAssetStore())
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent<EditorAssetSelection>("CategoryChanged", static (ctx, state, ev) => OnFillAssetStore(ev))
            .RegisterEvent<AssetObjectViewModel?>("SelectionChanged", static (ctx, state, ev) => OnFillAssetFiles(ev))
            .Build();


        CameraModelState = ModelState<CameraViewModel>
            .CreateBuilder(static () => new CameraViewModel())
            .OnEnter(static (ctx, it) =>
            {
                var gen = it.Generation;
                OnFetchCameraData();
                CameraPropertyComponent.UpdateStateFromViewModel(gen > 0);
            })
            .Build();
    }
}