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

public readonly unsafe struct ApiWriteRequest<T>(
    delegate*<ref T, void> writeTo,
    delegate*<in T, void> writeFrom)
    where T : unmanaged
{
    public void WriteTo(ref T data) => writeTo(ref data);
    public void WriteFrom(in T data) => writeFrom(in data);
}



public readonly unsafe struct ApiFillRequest<TReq, TRes> where TReq : unmanaged where TRes : unmanaged
{
    private readonly delegate*<in TReq, out TRes, void> _handler;
    public ApiFillRequest(delegate*<in TReq, out TRes, void> handler) => _handler = handler;
    public void DispatchFill(in TReq request, out TRes response) => _handler(in request, out response);
}