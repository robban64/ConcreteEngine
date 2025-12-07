#region

using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class AssetState
{
    public EditorAssetCategory Category { get; private set; }
    
    public EditorFileAssetModel[] FileAssets { get; private set; } = [];

    public ReadOnlySpan<EditorAssetResource> GetAssetSpan()
    {
        if (Category == EditorAssetCategory.None) return ReadOnlySpan<EditorAssetResource>.Empty;
        return EditorManagedStore.GetAssetSpanByCategory(Category);
    }

    public void SetCategory(EditorAssetCategory? category) => Category = category ?? Category;

    public void SetFileAssets(EditorFileAssetModel[] fileAssets) => FileAssets = fileAssets;

    public void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) Category = EditorAssetCategory.None;
        FileAssets = [];
    }
}