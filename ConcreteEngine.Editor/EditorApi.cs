#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public enum EditorMouseAction : byte
{
    None,
    MouseSelectEntity,
    MouseDragEntityTerrain
}

public struct EditorWorldMouseData(EditorMouseAction action, Vector2 mousePosition)
{
    public Ray Ray;
    public BoundingBox HitBox;
    public Vector3 WorldPosition;
    public Vector2 MousePosition = mousePosition;
    public int EntityId;
    public EditorMouseAction Action = action;
    
    public void Deconstruct(out Ray ray, out BoundingBox hitBox, out Vector3 worldPosition, out Vector2 mousePosition, out int entityId, out EditorMouseAction action)
    {
        ray = Ray;
        hitBox = HitBox;
        worldPosition = WorldPosition;
        mousePosition = MousePosition;
        entityId = EntityId;
        action = Action;
    }
}

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class EditorApi
{
    public static ApiModelRequestDel<AssetCategoryRequestBody, List<AssetObjectViewModel>> FetchAssetStoreData = null!;
    public static ApiModelRequestDel<AssetRequestBody, List<AssetObjectFileViewModel>> FetchAssetObjectFiles = null!;
    public static ApiModelRequestDel<EntityRequestBody, List<EntityRecord>> FetchEntityView = null!;

    public static ApiDataRequest<EditorWorldMouseData> SendClickRequest = null!;

    public static ApiDataRefRequest<CameraEditorPayload> CameraApi;
    public static ApiDataRefRequest<EntityDataPayload> EntityApi;
    public static ApiDataRefRequest<WorldParamState> WorldParamsApi;
}