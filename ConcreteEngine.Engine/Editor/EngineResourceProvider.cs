#region

using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Editor.Controller;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineResourceProvider
{
    private static AssetSystem _assetSystem = null!;
    private static EntityApiController _entityController = null!;
    private static InteractionController _interactionController = null!;


    internal static void Attach(AssetSystem assetSystem, EntityApiController entityController,
        InteractionController interactionController)
    {
        _assetSystem = assetSystem;
        _entityController = entityController;
        _interactionController = interactionController;
    }

    private static bool _hasInitAssets = false;

    public static List<EditorAssetResource> CreateEditorAssets()
    {
        if (_assetSystem is null) throw new InvalidOperationException("EngineDataProvider is not initialized.");
        if (_hasInitAssets) throw new InvalidOperationException("CreateEditorAssets has already been executed.");

        var store = _assetSystem.StoreImpl;
        var result = new List<EditorAssetResource>(store.Count);
        foreach (var obj in store.AssetValues)
            result.Add(EditorObjectMapper.MakeAssetObjectModel(obj));


        _hasInitAssets = true;

        return result;
    }

    public static EditorFileAssetModel[] GetAssetObjectFiles(EditorFetchHeader header)
    {
        var assetTypedId = new AssetId(header.EditorId);
        var store = _assetSystem.StoreImpl;
        store.TryGetFileIds(assetTypedId, out var fileIds);

        if (!store.TryGetByAssetId(assetTypedId, out var asset))
            return [];

        //var meta = store.GetMetaSnapshot(asset!.GetType());
        var result = new EditorFileAssetModel[fileIds.Length];
        for (var i = 0; i < fileIds.Length; i++)
        {
            var fileId = fileIds[i];
            store.TryGetFileEntry(fileId, out var entry);
            result[i] = EditorObjectMapper.MakeAssetObjectFile(entry!);
        }

        return result;
    }

    public static List<EditorEntityResource> CreateEntityList() => _entityController.CreateEntityList();

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