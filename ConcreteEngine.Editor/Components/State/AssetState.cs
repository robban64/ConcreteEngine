#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class AssetState
{
    public EditorAssetCategory Category { get; set; }
    public List<EditorFileAssetModel> FileAssets { get; set; } = [];

    public ReadOnlySpan<EditorAssetResource> GetAssetSpan()
    {
        if (Category == EditorAssetCategory.None) return ReadOnlySpan<EditorAssetResource>.Empty;
        return EditorManagedStore.GetAssetSpanByCategory(Category);
    }

    public void Refresh() => SetCategory(Category);

    public void SetCategory(EditorAssetCategory? category)
    {
        if (category is not { } assetCategory) return;
        if (assetCategory == Category) return;
        Category = assetCategory;
    }

    public void GetFileAssets(EditorAssetResource? asset, ApiEditorRequestDel<List<EditorFileAssetModel>> api)
    {
        if (asset is null)
        {
            FileAssets.Clear();
            return;
        }

        api(new EditorFetchHeader(asset.Id), FileAssets);
    }

    public void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) Category = EditorAssetCategory.None;
        FileAssets.Clear();
    }
}