#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static GenericRequest<EditorAssetSelection, List<AssetObjectViewModel>>? FillAssetStoreView { get; set; }
    public static GenericRequest<int, List<AssetObjectFileViewModel>>? FillAssetObjectFiles { get; set; }
    public static GenericRequest<int, List<EntityViewModel>>? FillEntityView { get; set; }

    public static GenericDataRequest<long, CameraEditorPayload>? FetchCameraData { get; set; }
    public static GenericDataRequest<int, EntityDataPayload>? FetchEntityData { get; set; }
}