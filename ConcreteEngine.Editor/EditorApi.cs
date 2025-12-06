#region

using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.RenderData;

#endregion

namespace ConcreteEngine.Editor;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
public static class EditorApi
{
    public static ApiModelRequestDel<AssetCategoryRequestBody, List<AssetObjectViewModel>> FetchAssets = null!;
    public static ApiModelRequestDel<AssetRequestBody, List<AssetObjectFileViewModel>> FetchAssetDetailed = null!;

    public static ApiModelRequestDel<EntityRequestBody, List<EntityRecord>> FetchEntities = null!;


    public static ApiDataRequest<EditorWorldMouseData> SendEditorMouseRequest = null!;

    public static ApiRefRequest<CameraDataState> CameraApi = null!;
    public static ApiRefRequest<EntityDataState> EntityApi = null!;
    public static ApiRefRequest<WorldParamsData> WorldParamsApi = null!;

}