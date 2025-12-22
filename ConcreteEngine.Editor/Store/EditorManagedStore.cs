using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Store;

internal static class EditorManagedStore
{
    private readonly record struct ResourceNameKey(string Name, EditorItemType ItemType);

    private static readonly Range32[] AssetRanges = new Range32[Enum.GetValues<EditorAssetCategory>().Length];

    private static readonly Dictionary<EditorId, EditorResource> Resources = new(512);
    private static readonly Dictionary<ResourceNameKey, EditorId> ByName = new(512);

    private static List<EditorAssetResource> _assetResources = [];
    private static List<EditorEntityResource> _entityResources = [];


    public static int Count => Resources.Count;

    public static ReadOnlySpan<EditorAssetResource> AssetResourceSpan => CollectionsMarshal.AsSpan(_assetResources);
    public static ReadOnlySpan<EditorEntityResource> EntityResourceSpan => CollectionsMarshal.AsSpan(_entityResources);

    public static ReadOnlySpan<EditorAssetResource> GetAssetSpanByCategory(EditorAssetCategory category)
    {
        var range = AssetRanges[(int)category];
        if (range.Length == 0) return ReadOnlySpan<EditorAssetResource>.Empty;
        if (range.Offset + range.Length > _assetResources.Count)
            throw new IndexOutOfRangeException();

        return AssetResourceSpan.Slice(range.Offset, range.Length);
    }

    public static void Initialize()
    {
        var assets = EditorApi.LoadAssetResources();
        var entities = EditorApi.LoadEntityResources();

        int totalCount = assets.Count + entities.Count;
        Resources.EnsureCapacity(totalCount);
        ByName.EnsureCapacity(totalCount);

        StoreRange(CollectionsMarshal.AsSpan(assets));
        StoreRange(CollectionsMarshal.AsSpan(entities));

        _assetResources = assets;
        _entityResources = entities;

        assets.Sort();
        entities.Sort();

        CreateAssetRanges();

        return;

        static void CreateAssetRanges()
        {
            var assetSpan = CollectionsMarshal.AsSpan(_assetResources);
            var prevCategory = EditorAssetCategory.None;
            var startIndex = 0;
            for (int i = 1; i < assetSpan.Length; i++)
            {
                var category = assetSpan[i].AssetCategory;
                if (assetSpan[i].AssetCategory == prevCategory) continue;
                AssetRanges[(int)prevCategory] = (startIndex, i - startIndex);

                prevCategory = category;
                startIndex = i;
            }

            AssetRanges[(int)prevCategory] = (startIndex, assetSpan.Length - startIndex);
        }
    }


    public static void Register(EditorResource resource)
    {
        Resources[resource.Id] = resource;
        if (!string.IsNullOrEmpty(resource.Name))
            ByName[new ResourceNameKey(resource.Name, resource.Id.ItemType)] = resource.Id;
    }

    public static void Unregister(EditorId id)
    {
        if (Resources.TryGetValue(id, out var res))
        {
            ByName.Remove(new ResourceNameKey(res.Name, id.ItemType));
            Resources.Remove(id);
        }
    }

    public static EditorResource? Get(EditorId id)
    {
        return Resources.TryGetValue(id, out var res) ? res : null;
    }

    public static bool TryGet<T>(EditorId id, out T t) where T : EditorResource
    {
        if (Resources.TryGetValue(id, out var res) && res is T tRes)
        {
            t = tRes;
            return true;
        }

        t = null!;
        return false;
    }

    public static T? Get<T>(EditorId id) where T : EditorResource
    {
        return Resources.TryGetValue(id, out var res) ? res as T : null;
    }

    public static EditorId FindId(string name, EditorItemType type)
    {
        return ByName.TryGetValue(new ResourceNameKey(name, type), out var id) ? id : EditorId.Empty;
    }

    public static void StoreRange(ReadOnlySpan<EditorResource> resources)
    {
        foreach (var res in resources)
            Register(res);
    }

    public static void StoreRange(IReadOnlyList<EditorResource> resources)
    {
        for (var i = 0; i < resources.Count; i++)
            Register(resources[i]);
    }

    internal static void Clear()
    {
        Resources.Clear();
        ByName.Clear();
    }
}