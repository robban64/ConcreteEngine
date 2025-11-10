using ConcreteEngine.Common;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.ViewModel;
using static ConcreteEngine.Editor.EditorService;

namespace ConcreteEngine.Editor;

internal static class EditorStateManager
{
    public static ModelState<EntitiesViewModel> EntityModelState { get; private set; } = null!;
    public static ModelState<AssetStoreViewModel> AssetModelState { get; private set;}= null!;
    public static ModelState<CameraViewModel> CameraModelState { get; private set;}= null!;

    public static bool HasInit { get; private set; } = false;
    
    static EditorStateManager()
    {
        
    }
    
    public static void SetupModelState()
    {
        InvalidOpThrower.ThrowIf(HasInit, nameof(EntityModelState));

        HasInit = true;
        
        EntityModelState = ModelState<EntitiesViewModel>
            .CreateBuilder(static () => new EntitiesViewModel())
            .OnEnter(static (ctx, it) => OnFillEntities(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.SelectionChanged,
                static (ModelState<EntitiesViewModel> ctx, in EntityRecord? ev) => OnFetchEntityData(ctx, ev))
            .RegisterEvent(EventKey.SelectionUpdated,
                static (ModelState<EntitiesViewModel> ctx, in EntityTransformPayload ev) =>
                {
                    StateCtx.ExecuteSetEntityTransform(in ev);
                })
            .Build();

        AssetModelState = ModelState<AssetStoreViewModel>
            .CreateBuilder(static () => new AssetStoreViewModel())
            .OnEnter(static (ctx, it) => OnFillAssetStore(ctx))
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.CategoryChanged,
                static (ModelState<AssetStoreViewModel> ctx, in EditorAssetCategory ev) => OnFillAssetStore(ctx, ev))
            .RegisterEvent(EventKey.SelectionChanged,
                static (ModelState<AssetStoreViewModel> ctx, in AssetObjectViewModel? ev) => { OnFillAssetFiles(ctx,ev); })
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
                OnFetchCameraData(ctx);
                //CameraPropertyComponent.UpdateStateFromViewModel(gen > 0);
            })
            .OnLeave(static (ctx, it) => ctx.ResetState())
            .RegisterEvent(EventKey.SelectionUpdated,
                static (ModelState<CameraViewModel> ctx, in CameraEditorPayload ev) =>
                {
                    StateCtx.ExecuteSetCameraTransform(in ev);
                })
            .Build();
    }

}