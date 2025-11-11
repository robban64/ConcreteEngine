#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
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
    
    public static List<EntityRecord> GetEntityView(EntityRequestBody body)
    {
        if (_world is null) return [];

        var result = new List<EntityRecord>(_world.Meshes.Count);
        foreach (var it in _world.Query<ModelComponent>())
        {
            result.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity));
        }

        return result;
    }
    
    public static List<AssetObjectViewModel> GetAssetStoreData(AssetCategoryRequestBody body)
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
        if (_assetSystem is null) return [];

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
    
    public static void SetCameraData(ref CameraEditorPayload payload)
    {
        if (_world is null || _world.Camera.Generation == payload.Generation)
            return;
        
        var camera = _world.Camera;
        payload.Generation = camera.Generation;
        payload.ViewTransform = new ViewTransformData(camera.Translation, camera.Scale, camera.Orientation);
        payload.Projection = new ProjectionInfoData(camera.AspectRatio, camera.Fov, camera.NearPlane, camera.FarPlane);
        payload.Viewport = camera.Viewport;
    }


    public static void SetEntityData(ref EntityDataPayload response)
    {
        if (_world is null) return;
        var entity = new EntityId(response.EntityId);
        var model = _world.Meshes.GetById(entity);
        if (!_world.Transforms.TryGetById(entity, out var transform)) transform = default;

        response.Transform = new TransformData(in transform.Translation, in transform.Scale, in transform.Rotation);
        response.Model = new EditorEntityModel(model.Model, model.MaterialKey.Value, model.DrawCount);
    }
    
    
    public static void SetWorldParams(ref WorldParamState data)
    {
        var snapshot = _world!.WorldRenderParams.Snapshot;
        data.LightState.DirectionalLight = new DirLightState(in snapshot.DirLight);
        data.LightState.AmbientLight = new AmbientState(in snapshot.Ambient);
        data.FogState = new FogState(in snapshot.Fog);
        data.PostState.Grade = new PostGradeState(in snapshot.PostEffects.Grade);
        data.PostState.WhiteBalance = new PostWhiteBalanceState(snapshot.PostEffects.WhiteBalance);//tiny
        data.PostState.Bloom = new PostBloomState(in snapshot.PostEffects.Bloom);
        data.PostState.ImageFx = new PostImageFxState(in snapshot.PostEffects.ImageFx);
    }
}