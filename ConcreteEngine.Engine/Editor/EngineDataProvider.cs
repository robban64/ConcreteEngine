#region

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Editor.Controller;
using ConcreteEngine.Engine.Platform;
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
    private static EntityApiController _entityController = null!;
    private static WorldApiController _worldController = null!;
    private static InteractionController _interactionController = null;


    internal static void Attach(World world, AssetSystem assetSystem,
        EntityApiController entityController, WorldApiController worldController, InteractionController interactionController)
    {
        _world = world;
        _assetSystem = assetSystem;
        _entityController = entityController;
        _worldController = worldController;
        _interactionController = interactionController;
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
    
    public static List<EntityRecord> GetEntityView(EntityRequestBody body)
    {
        return _entityController.GetEntityList();
    }
    public static long FillEntityData(ApiWriteRequestBody<EntityDataPayload> response)
    {
        return _entityController.FillEntityData(ref response.Data);
    }

    public static long WriteToEntity(ApiWriteRequestBody<EntityDataPayload> response)
    {
        return _entityController.WriteToEntity(response.Version, ref response.Data);
    }

    public static long FillCameraData(ApiWriteRequestBody<CameraEditorPayload> payload)
    {
        return _worldController.FillCameraData(payload.Version, ref payload.Data);
    }

    public static long WriteCameraData(ApiWriteRequestBody<CameraEditorPayload> payload)
    {
        return _worldController.WriteCameraData(payload.Version, ref payload.Data);
    }


    public static long FillWorldParams(ApiWriteRequestBody<WorldParamState> request)
    {
        return _worldController.FillWorldParams(request.Version, ref request.Data);
    }

    public static long WriteWorldParams(ApiWriteRequestBody<WorldParamState> request)
    {
        return _worldController.WriteWorldParams(request.Version, ref request.Data);
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