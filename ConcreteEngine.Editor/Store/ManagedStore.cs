using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    private readonly record struct ResourceNameKey(string Name, EditorItemType ItemType);
    
    public static readonly Range32[] AssetRanges = new Range32[Enum.GetValues<EditorAssetCategory>().Length];

    private static readonly Dictionary<EditorId, EditorResource> Resources = [];
    private static readonly Dictionary<ResourceNameKey, EditorId> ByName = [];

    private static List<EditorEntityResource> _entityResources = [];
    private static List<EditorSceneObject> _sceneObjects = [];
    private static List<EditorAssetResource> _assetResources = [];

    public static int Count => Resources.Count;


    public static void LoadResources()
    {
        Loader.LoadAll();
    }
    
    public static T? Get<T>(EditorId id) where T : EditorResource
    {
        return Resources.TryGetValue(id, out var res) ? res as T : null;
    }

    public static EditorResource? Get(EditorId id)
    {
        return Resources.TryGetValue(id, out var res) ? res : null;
    }

    public static bool TryGet<T>(EditorId id, out T t) where T : EditorResource?
    {
        t = null!;
        if (!Resources.TryGetValue(id, out var res) || res is not T tRes) return false;
        t = tRes;
        return true;
    }

    public static EditorId FindId(string name, EditorItemType type)
    {
        return ByName.TryGetValue(new ResourceNameKey(name, type), out var id) ? id : EditorId.Empty;
    }

    public static void Register(EditorResource resource)
    {
        Resources[resource.Id] = resource;
        if (!string.IsNullOrEmpty(resource.Name))
            ByName[new ResourceNameKey(resource.Name, resource.Id.ItemType)] = resource.Id;
    }

    public static bool Unregister(EditorId id)
    {
        if (!Resources.TryGetValue(id, out var res))return false;

        ByName.Remove(new ResourceNameKey(res.Name, id.ItemType));
        Resources.Remove(id);
        return true;
    }

    internal static void Clear()
    {
        Resources.Clear();
        ByName.Clear();
        
        _assetResources.Clear();
        _sceneObjects.Clear();
        
        _assetResources = [];
        _sceneObjects = [];
    }
}