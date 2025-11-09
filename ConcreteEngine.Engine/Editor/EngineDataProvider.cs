#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataProvider
{
    private static World? _world;
    private static AssetSystem? _assetSystem;


    internal static void Attach(World world, AssetSystem assetSystem)
    {
        _world = world;
        _assetSystem = assetSystem;
    }
    
    public static List<EntityViewModel> GetEntityView(int entityId)
    {
        if (_world is null) return [];

        var result = new List<EntityViewModel>(_world.Meshes.Count);
        foreach (var it in _world.Query<ModelComponent>())
        {
            result.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity));
        }

        return result;
    }
    
    public static List<AssetObjectViewModel> GetAssetStoreData(EditorAssetSelection req)
    {
        if (_assetSystem is null || req == EditorAssetSelection.None) return [];
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
    
    public static List<AssetObjectFileViewModel> GetAssetObjectFiles(int assetId)
    {
        if (_assetSystem is null) return [];

        var assetTypedId = new AssetId(assetId);
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
    
    public static bool PullCameraData(long generation, out CameraEditorPayload response)
    {
        if (_world is null || _world.Camera.Generation == generation)
        {
            response = default;
            return false;
        }

        var camera = _world.Camera;
        var transform = new ViewTransformData(camera.Translation, camera.Scale, camera.Orientation);
        var proj = new ProjectionInfoData(camera.AspectRatio, camera.Fov, camera.NearPlane, camera.FarPlane);
        var viewport = camera.Viewport;
        response = new CameraEditorPayload(camera.Generation, in transform, in proj, in viewport);
        return true;
    }


    public static bool PullEntityData(int entityId, out EntityDataPayload response)
    {
        if (_world is null)
        {
            response = default;
            return false;
        }

        var entity = new EntityId(entityId);
        var model = _world.Meshes.GetById(entity);
        if (!_world.Transforms.TryGetById(entity, out var transform)) transform = default;

        var transformData =
            new TransformData(in transform.Translation, in transform.Scale, in transform.Rotation);

        var modelData = new EditorEntityModel(model.Model, model.MaterialKey.Value, model.DrawCount);

        response = new EntityDataPayload(entityId, in modelData, in transformData);
        return true;
    }
}