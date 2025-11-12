#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.ViewModel;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static GenericRequest<AssetCategoryRequestBody, List<AssetObjectViewModel>>? FetchAssetStoreData;
    public static GenericRequest<AssetRequestBody, List<AssetObjectFileViewModel>>? FetchAssetObjectFiles;
    public static GenericRequest<EntityRequestBody, List<EntityRecord>>? FetchEntityView;

    public static ApiWriteRequest<CameraEditorPayload> UpdateCameraData;
    public static ApiWriteRequest<EntityDataPayload> UpdateEntityData;
    public static ApiWriteRequest<WorldParamState> UpdateWorldParams;
}

public ref struct ApiWriteRequestBody<TReq>(long version, ref TReq data) where TReq : unmanaged
{
    public readonly long Version = version;
    public ref TReq Data = ref data;
}

public readonly unsafe struct ApiWriteRequest<T>(
    delegate*<ApiWriteRequestBody<T>, long> writeTo,
    delegate*<ApiWriteRequestBody<T>, long> writeFrom)
    where T : unmanaged
{
    public long WriteTo(long version, ref T data) => writeTo(new ApiWriteRequestBody<T>(version, ref data));
    public long WriteFrom(long version, ref T data) => writeFrom(new ApiWriteRequestBody<T>(version, ref data));
}
