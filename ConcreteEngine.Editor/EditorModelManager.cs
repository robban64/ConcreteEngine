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
            .OnEnter(RefreshEntityState)
            .OnRefresh(RefreshEntityState)
            .OnLeave(static (ctx, _) => ctx.TriggerEvent<EntityRecord?>(EventKey.SelectionChanged, null))
            .RegisterEvent<EntityRecord?>(EventKey.SelectionChanged, OnEntitySelected)
            .RegisterEvent<EntityRecord>(EventKey.SelectionUpdated, OnEntityUpdated)
            .RegisterEvent<EditorMouseSelectPayload>(EventKey.MouseClick, OnClickEvent)
            .KeepAlive()
            .Build();

        return;

        static void RefreshEntityState(ModelState<EntitiesViewModel> ctx, EntitiesViewModel state)
        {
            if (state.Entities.Count == 0)
            {
                state.SetSelectedEntity(0);
                state.FillView(EditorApi.FetchEntityView);
                return;
            }

          //  if (state.Data.EntityId == 0)
                //state.SetSelectedEntity(0);

            state.FillData(in EditorApi.EntityApi);
        }
        
        static void OnEntitySelected(ModelState<EntitiesViewModel> ctx, EntityRecord? it)
        {
            ctx.State!.SetSelectedEntity(it?.EntityId ?? 0);
            ctx.EnqueueRefreshNextFrame();
        }
        
        static void OnEntityUpdated(ModelState<EntitiesViewModel> ctx, EntityRecord it)
        {
            ctx.State!.SetSelectedEntity(it.EntityId);
            ctx.State!.WriteData(in EditorApi.EntityApi);
            ctx.EnqueueRefreshNextFrame();
        }

        static void OnClickEvent(ModelState<EntitiesViewModel> ctx, EditorMouseSelectPayload payload)
        {
            EditorApi.SendClickRequest(in payload, out var response);
            var state = ctx.State!;

            if (response.EntityId == 0)
            {
                if (state.SelectedEntity is null) return;
                state.SetSelectedEntity(0);
                ctx.EnqueueRefreshNextFrame();
            }
            else if (response.EntityId == state.Data.EntityId)
            {
                //state.FillData(state.GetSelectedEntity(), in EditorApi.FillEntityData);
            }
            else
            {
                ctx.TriggerEvent(EventKey.SelectionChanged, state.FindEntity(response.EntityId));
            }
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
            ctx.State!.WriteData(in EditorApi.CameraApi);
            ctx.EnqueueRefreshNextFrame();
        }

        static void FetchCameraDataHandler(ModelState<CameraViewModel> ctx, CameraViewModel it) =>
            it.FillData(EditorApi.CameraApi);
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
            it.FillData(in EditorApi.WorldParamsApi);

        static void SelectionChangeHandler(ModelState<WorldRenderViewModel> ctx, WorldParamSelection it)
        {
            ctx.State!.Selection = it;
            ctx.State!.FillData(in EditorApi.WorldParamsApi);
        }

        static void SelectionUpdateHandler(ModelState<WorldRenderViewModel> ctx)
        {
            ctx.State!.WriteData(in EditorApi.WorldParamsApi);
            ctx.EnqueueRefreshNextFrame();
        }
    }
}