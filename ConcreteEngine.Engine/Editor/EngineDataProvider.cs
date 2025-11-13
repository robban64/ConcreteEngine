#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Shared.RenderData;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Editor;

internal static class EngineDataProvider
{
    private static World _world = null!;
    private static AssetSystem _assetSystem = null!;


    internal static void Attach(World world, AssetSystem assetSystem)
    {
        _world = world;
        _assetSystem = assetSystem;
    }

    public static List<EntityRecord> GetEntityView(EntityRequestBody body)
    {
        var result = new List<EntityRecord>(_world.Meshes.Count);
        foreach (var it in _world.Query<ModelComponent>())
            result.Add(EditorObjectMapper.MakeEntityViewModel(it.Entity));
        
        result.Sort();
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

    public static long FillCameraData(ApiWriteRequestBody<CameraEditorPayload> payload)
    {
        var camera = _world.Camera;
        if (camera.Generation == payload.Version) return camera.Generation;

        payload.Data.Generation = camera.Generation;
        payload.Data.ViewTransform = new ViewTransformData(camera.Translation, camera.Scale, camera.Orientation);
        payload.Data.Projection = new ProjectionInfoData(camera.AspectRatio, camera.Fov, camera.NearPlane, camera.FarPlane);
        payload.Data.Viewport = camera.Viewport;
        return camera.Generation;
    }

    public static long WriteCameraData(ApiWriteRequestBody<CameraEditorPayload> payload)
    {
        if (_world.Camera.Generation == payload.Version) return payload.Version;
        var camera = _world.Camera;
        WorldActionSlot.SetSlot(payload.Version, in payload.Data);
        return camera.Generation;
    }

    public static long FillEntityData(ApiWriteRequestBody<EntityDataPayload> response)
    {
        var entity = new EntityId(response.Data.EntityId);
        var model = _world.Meshes.GetById(entity);
        if (!_world.Transforms.TryGetById(entity, out var transform)) transform = default;

        response.Data.Transform = new TransformData(in transform.Translation, in transform.Scale, in transform.Rotation);
        response.Data.Model = new EditorEntityModel(model.Model, model.MaterialKey.Value, model.DrawCount);

        return response.Data.EntityId;
    }

    public static long WriteToEntity(ApiWriteRequestBody<EntityDataPayload> response)
    {
        WorldActionSlot.SetSlot(response.Version, in response.Data);
        return response.Data.EntityId;
    }

    public static long FillWorldParams(ApiWriteRequestBody<WorldParamState> request)
    {
        var snapshot = _world!.WorldRenderParams.Snapshot;
        if (request.Version == snapshot.Version) return request.Version;

        ref var data = ref request.Data;
        data.LightState.DirectionalLight = new DirLightState(in snapshot.DirLight);
        data.LightState.AmbientLight = new AmbientState(in snapshot.Ambient);
        data.FogState = new FogState(in snapshot.Fog);
        data.PostState.Grade = new PostGradeState(in snapshot.PostEffects.Grade);
        data.PostState.WhiteBalance = new PostWhiteBalanceState(snapshot.PostEffects.WhiteBalance); //tiny
        data.PostState.Bloom = new PostBloomState(in snapshot.PostEffects.Bloom);
        data.PostState.ImageFx = new PostImageFxState(in snapshot.PostEffects.ImageFx);

        return snapshot.Version;
    }

    public static long WriteWorldParams(ApiWriteRequestBody<WorldParamState> request)
    {
        var snapshot = _world!.WorldRenderParams.Snapshot;
        if (request.Version == snapshot.Version) return snapshot.Version;

        ref var data = ref request.Data;
        ref var slot = ref WorldActionSlot.WriteSlot<WorldParamsData>(request.Version);

        slot.DirLight = Unsafe.As<DirLightState, DirLightParams>(ref data.LightState.DirectionalLight);
        slot.Ambient = Unsafe.As<AmbientState, AmbientParams>(ref data.LightState.AmbientLight);
        slot.Fog = Unsafe.As<FogState, FogParams>(ref data.FogState);
        slot.PostEffect = Unsafe.As<PostEffectState, PostEffectParams>(ref data.PostState);
        return snapshot.Version;
    }
}