using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    private static readonly Dictionary<EditorId, EditorResource> Resources = [];

    private static List<EditorEntityResource> _entityResources = [];
    private static List<EditorSceneObject> _sceneObjects = [];

    public static int Count => Resources.Count;


    public static void LoadResources()
    {
        Loader.LoadAll();
    }

    public static T? Get<T>(EditorId id) where T : EditorResource
    {
        return Resources.TryGetValue(id, out var res) ? res as T : null;
    }
    public static bool TryGet<T>(EditorId id, out T t) where T : EditorResource?
    {
        t = null!;
        if (!Resources.TryGetValue(id, out var res) || res is not T tRes) return false;
        t = tRes;
        return true;
    }

    public static void Register(EditorResource resource)
    {
        Resources[resource.Id] = resource;
    }

    public static bool Unregister(EditorId id)
    {
        return Resources.Remove(id);
    }

    internal static void Clear()
    {
        Resources.Clear();
        _sceneObjects.Clear();
        _sceneObjects = [];
    }
}