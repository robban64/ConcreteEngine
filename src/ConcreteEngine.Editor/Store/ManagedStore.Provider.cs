using System.Runtime.InteropServices;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store.Resources;

namespace ConcreteEngine.Editor.Store;

internal static partial class ManagedStore
{
    public static ReadOnlySpan<EditorEntityResource> EntitySpan => CollectionsMarshal.AsSpan(_entityResources);
    public static ReadOnlySpan<EditorSceneObject> SceneObjectSpan => CollectionsMarshal.AsSpan(_sceneObjects);

    public static ReadOnlySpan<EditorAssetResource> GetAssetSpanByCategory(EditorAssetCategory category)
    {
        var span = CollectionsMarshal.AsSpan(_assetResources);

        var range = AssetRanges[(int)category];
        if (range.Length == 0) return ReadOnlySpan<EditorAssetResource>.Empty;
        if (range.Offset + range.Length > span.Length)
            throw new IndexOutOfRangeException();

        return span.Slice(range.Offset, range.Length);
    }
}