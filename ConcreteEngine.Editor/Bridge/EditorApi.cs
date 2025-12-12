#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Bridge;

public static class EditorApi
{
    public static IEngineEntityController EntityController = null!;
    public static IEngineInteractionController InteractionController = null!;

    public static Func<List<EditorAssetResource>> LoadAssetResources = null!;
    public static Func<List<EditorEntityResource>> LoadEntityResources = null!;
    public static Func<List<EditorParticleResource>> LoadParticleResources = null!;
    public static Func<List<EditorAnimationResource>> LoadAnimationResources = null!;
    public static ApiEditorRequestDel<EditorFileAssetModel[]> FetchAssetDetailed = null!;


}