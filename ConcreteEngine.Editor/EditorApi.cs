#region

using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class EditorApi
{
    public static ApiModelRequestDel<AssetCategoryRequestBody, List<AssetObjectViewModel>> FetchAssetStoreData = null!;
    public static ApiModelRequestDel<AssetRequestBody, List<AssetObjectFileViewModel>> FetchAssetObjectFiles = null!;
    public static ApiModelRequestDel<EntityRequestBody, List<EntityRecord>> FetchEntityView = null!;


    public static ApiDataRequest<EditorWorldMouseData> SendEditorMouseRequest = null!;

    public static ApiDataRefRequest<CameraEditorPayload> CameraApi;
    public static ApiDataRefRequest<EntityDataPayload> EntityApi;
    public static ApiDataRefRequest<WorldParamState> WorldParamsApi;
}