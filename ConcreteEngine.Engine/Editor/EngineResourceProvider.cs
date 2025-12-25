using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Editor;

internal static class EngineResourceProvider
{
    private static AssetSystem _assetSystem = null!;
    private static EntityApiController _entityController = null!;
    private static WorldApiController _worldApiController = null!;
    private static SceneApiController _sceneApiController = null!;

    internal static void Attach(AssetSystem assetSystem, EntityApiController entityController,
         WorldApiController worldApiController, SceneApiController sceneApiController)
    {
        _assetSystem = assetSystem;
        _entityController = entityController;
        _worldApiController = worldApiController;
        _sceneApiController = sceneApiController;
    }


    public static List<EditorAssetResource> GetEditorAssets()
    {
        if (_assetSystem is null) throw new InvalidOperationException("EngineDataProvider is not initialized.");

        var store = _assetSystem.Store;
        var result = new List<EditorAssetResource>(store.Count);
        foreach (var obj in store.AssetValues)
            result.Add(EditorObjectMapper.MakeAssetObjectModel(obj));


        Logger.LogString(LogScope.Engine, $"Editor asset loaded - {result.Count}");
        return result;
    }

    public static EditorFileAssetModel[] GetEditorAssetFiles(EditorFetchHeader header)
    {
        var assetTypedId = new AssetId(header.EditorId);
        var store = _assetSystem.Store;
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
    
    public static List<EditorSceneObject> GetSceneObjects()
    {
        var records = _sceneApiController.CreateSceneObjectList();
        Logger.LogString(LogScope.Engine, $"Editor.SceneObject loaded - {records.Count}");
        return records;
    }

    public static List<EditorEntityResource> CreateEntityList()
    {
        var entities = _entityController.CreateEntityList();
        Logger.LogString(LogScope.Engine, $"Editor Entities loaded - {entities.Count}");
        return entities;
    }

    public static List<EditorParticleResource> GetParticleResources()
    {
        return _worldApiController.GetEditorEmitter();
    }

    public static List<EditorAnimationResource> GetAnimationResources()
    {
        return _worldApiController.GetEditorAnimations();
    }


}