#region

using System.Diagnostics.CodeAnalysis;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Shared.Rendering;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static Func<List<EditorAssetResource>> LoadAssetResources = null!;
    public static Func<List<EditorEntityResource>> LoadEntityResources = null!;

    public static ApiEditorRequestDel<EditorFileAssetModel[]> FetchAssetDetailed = null!;
    //public static ApiEditorRequestDel<EditorAssetResource> FetchAssets = null!;
    //public static ApiEditorRequestDel<EditorEntityResource> FetchEntities = null!;

    public static ApiDataRequest<EditorWorldMouseData> SendEditorMouseRequest = null!;

    public static ApiRefRequest<CameraDataState> CameraApi = null!;
    public static ApiRefRequest<EntityDataState> EntityApi = null!;
    public static ApiRefRequest<WorldParamsData> WorldParamsApi = null!;

}