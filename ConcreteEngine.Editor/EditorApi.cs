#region

using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;

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

}