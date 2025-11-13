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
    public static GenericRequest<AssetCategoryRequestBody, List<AssetObjectViewModel>>? FetchAssetStoreData;
    public static GenericRequest<AssetRequestBody, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles;
    public static GenericRequest<EntityRequestBody, List<EntityRecord>>? FetchEntityView;

    public static ApiDataRequest<CameraEditorPayload> UpdateCameraData;
    public static ApiDataRequest<EntityDataPayload> UpdateEntityData;
    public static ApiDataRequest<WorldParamState> UpdateWorldParams;
}

public ref struct ApiWriteRequestBody<TReq>(long version, ref TReq data) where TReq : unmanaged
{
    public readonly long Version = version;
    public ref TReq Data = ref data;
}

public readonly unsafe struct ApiDataRequest<T>(
    delegate*<ApiWriteRequestBody<T>, long> fillData,
    delegate*<ApiWriteRequestBody<T>, long> writeData)
    where T : unmanaged
{
    public long FillData(long version, ref T data) => fillData(new ApiWriteRequestBody<T>(version, ref data));
    public long WriteData(long version, ref T data) => writeData(new ApiWriteRequestBody<T>(version, ref data));
}
