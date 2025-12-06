#region

using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Shared.RenderData;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataProvider
{
    private static World _world = null!;
    private static AssetSystem _assetSystem = null!;
    private static EntityApiController _entityController = null!;
    private static WorldApiController _worldController = null!;
    private static InteractionController _interactionController = null;


    internal static void Attach(World world, AssetSystem assetSystem,
        EntityApiController entityController, WorldApiController worldController,
        InteractionController interactionController)
    {
        _world = world;
        _assetSystem = assetSystem;
        _entityController = entityController;
        _worldController = worldController;
        _interactionController = interactionController;
    }


    public static List<AssetObjectViewModel> GetAssets(AssetCategoryRequestBody body)
    {
        var req = body.Category;
        if (_assetSystem is null || req == EditorAssetCategory.None) return [];
        var store = _assetSystem.StoreImpl;
        var type = EditorEnumMapper.AssetSelectionToType(req);
        var meta = store.GetMetaSnapshot(type);

        var result = new List<AssetObjectViewModel>(meta.Count);
        foreach (var obj in store.AssetValues)
        {
            if (obj.GetType() != type) continue;
            result.Add(EditorObjectMapper.MakeAssetObjectModel(obj));
        }

        result.Sort(static (a, b) => a.AssetId.CompareTo(b.AssetId));
        return result;
    }

    public static List<AssetObjectFileViewModel> GetAssetObjectFiles(AssetRequestBody body)
    {
        var assetTypedId = new AssetId(body.AssetId);
        var store = _assetSystem.StoreImpl;
        store.TryGetFileIds(assetTypedId, out var fileIds);

        if (!store.TryGetByAssetId(assetTypedId, out var asset))
            return [];

        var meta = store.GetMetaSnapshot(asset!.GetType());
        var result = new List<AssetObjectFileViewModel>(meta.Count);
        foreach (var fileId in fileIds)
        {
            store.TryGetFileEntry(fileId, out var entry);
            result.Add(EditorObjectMapper.MakeAssetObjectFile(entry!));
        }

        return result;
    }

    public static List<EntityRecord> GetEntities(EntityRequestBody body)
    {
        return _entityController.GetEntityList();
    }

    public static void OnEntityRequest(ref EditorDataRequest<EntityDataState> request)
    {
         _entityController.ProcessEntityRequest(ref request);
    }

    public static void OnCameraRequest(ref EditorDataRequest<CameraDataState> request)
    {
        _worldController.ProcessCameraRequest(ref request);
    }

    public static void OnWorldParamsRequest(ref EditorDataRequest<WorldParamsData> request)
    {
         _worldController.ProcessWorldParamsRequest(ref request);
    }

    public static void OnEditorClick(in EditorWorldMouseData request, out EditorWorldMouseData response)
    {
        switch (request.Action)
        {
            case EditorMouseAction.SelectEntity:
                response = request;
                response.EntityId = _interactionController.OnClick(request.MousePosition, out _, out _);
                break;
            case EditorMouseAction.DragEntityOverTerrain:
                response = request;
                response.EntityId = _interactionController.OnDragEntity(request.MousePosition);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}