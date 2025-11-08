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

    public static bool PullCameraView(long generation, out CameraEditorPayload response)
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

    public static void PullEntityView(EntityListViewModel viewModel)
    {
        if (_world is null) return;

        Transform defaultTransform = default;

        foreach (var it in _world.Query<ModelComponent>())
        {
            ref var model = ref it.Component;

            ref var transform = ref _world.Transforms.Has(it.Entity)
                ? ref _world.Transforms.GetById(it.Entity)
                : ref defaultTransform;

            viewModel.Entities.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity));
        }
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

    public static void PullAssetObjectFiles(AssetObjectViewModel asset, List<AssetObjectFileViewModel> result)
    {
        if (_assetSystem is null) return;
        var store = _assetSystem.StoreImpl;
        store.TryGetFileIds(new AssetId(asset.AssetId), out var fileIds);
        foreach (var fileId in fileIds)
        {
            store.TryGetFileEntry(fileId, out var entry);
            result.Add(EditorObjectMapper.MakeAssetObjectFile(entry!));
        }
    }

    public static void PullAssetStoreData(EditorAssetSelection selection, List<AssetObjectViewModel> result)
    {
        if (_assetSystem is null) return;
        if (selection == EditorAssetSelection.None) return;
        var store = _assetSystem.StoreImpl;
        var type = EditorEnumMapper.AssetSelectionToType(selection);

        foreach (var obj in store.AssetValues)
        {
            if (obj.GetType() != type) continue;
            result.Add(EditorObjectMapper.MakeAssetObjectModel(obj));
        }

        result.Sort(static (a, b) => a.AssetId.CompareTo(b.AssetId));
    }
}