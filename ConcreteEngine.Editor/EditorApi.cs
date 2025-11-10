#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static GenericRequest<AssetCategoryRequestBody, List<AssetObjectViewModel>>? FetchAssetStoreData { get; set; }
    public static GenericRequest<AssetRequestBody, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles { get; set; }
    public static GenericRequest<EntityRequestBody, List<EntityRecord>>? FetchEntityView { get; set; }

    public static GenericDataRequest<CameraEditorPayload>? UpdateCameraData { get; set; }
    public static GenericDataRequest<EntityDataPayload>? UpdateEntityData { get; set; }
}