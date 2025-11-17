#region

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public readonly struct EditorMouseSelectPayload(int entityId, Vector2 mousePosition)
{
    public int EntityId { get; } = entityId;
    public Vector2 MousePosition { get; } = mousePosition;
}

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class EditorApi
{
    public static ApiModelRequestDel<AssetCategoryRequestBody, List<AssetObjectViewModel>> FetchAssetStoreData;
    public static ApiModelRequestDel<AssetRequestBody, List<AssetObjectFileViewModel>> FetchAssetObjectFiles;
    public static ApiModelRequestDel<EntityRequestBody, List<EntityRecord>> FetchEntityView;

    public static ApiSimpleDataRequestDel<EditorMouseSelectPayload> SendClickRequest;

    public static ApiDataRefRequest<CameraEditorPayload> UpdateCameraData;
    public static ApiDataRefRequest<EntityDataPayload> UpdateEntityData;
    public static ApiDataRefRequest<WorldParamState> UpdateWorldParams;
}