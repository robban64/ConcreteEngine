#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{

    public static GenericFillRequest<FillAssetsPayload>? FillAssetStoreView { get; set; }
    public static GenericFillRequest<FillAssetFilePayload>? FillAssetObjectFiles { get; set; }
    public static GenericFillRequest<EntityListViewModel>? FillEntityView { get; set; }
    public static GenericDataRequest<long, CameraEditorPayload>? FetchCameraData { get; set; }
    public static GenericDataRequest<int,EntityDataPayload>? FetchEntityData { get; set; }

}