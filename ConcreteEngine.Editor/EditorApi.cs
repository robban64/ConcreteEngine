#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public enum EditorWorldMouseAction
{
    GetEntity,
    TerrainLocation
}

public readonly struct EditorWorldMouseData(
    EditorWorldMouseAction action,
    Vector2 mousePosition,
    int entityId = 0,
    in BoundingBox hitBox = default,
    in Vector3 worldPosition = default)
{
    public BoundingBox HitBox { get; init; } = hitBox;
    public Vector3 WorldPosition { get; init; } = worldPosition;
    public Vector2 MousePosition { get; init; } = mousePosition;
    public int EntityId { get; init; } = entityId;
    public EditorWorldMouseAction Action { get; init; } = action;

    public void Deconstruct(out int entityId, out Vector3 worldPosition, out Vector2 mousePosition,
        out EditorWorldMouseAction action)
    {
        entityId = EntityId;
        worldPosition = WorldPosition;
        mousePosition = MousePosition;
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