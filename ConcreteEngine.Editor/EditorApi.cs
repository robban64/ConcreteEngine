#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor;

public static class EditorApi
{
    public static Func<List<EditorAssetResource>> LoadAssetResources = null!;
    public static Func<List<EditorEntityResource>> LoadEntityResources = null!;
    public static Func<List<EditorParticleResource>> LoadParticleResources = null!;
    public static Func<List<EditorAnimationResource>> LoadAnimationResources = null!;

    public static ApiEditorRequestDel<EditorFileAssetModel[]> FetchAssetDetailed = null!;
}