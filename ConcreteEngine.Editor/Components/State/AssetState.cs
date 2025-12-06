#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class AssetState
{
    public EditorAssetCategory Category { get; set; }
    public List<AssetObjectViewModel> AssetObjects { get; set; } = [];
    public List<AssetObjectFileViewModel> AssetFileObjects { get; set; } = [];

    public void FillView(EditorAssetCategory? category,
        ApiModelRequestDel<AssetCategoryRequestBody, List<AssetObjectViewModel>> api)
    {
        if (category is { } assetCategory)
        {
            if (assetCategory == Category) return;
            Category = assetCategory;
        }

        AssetObjects = api(new AssetCategoryRequestBody(Category)) ?? [];
    }

    public void FillAssetFileView(AssetObjectViewModel? asset,
        ApiModelRequestDel<AssetRequestBody, List<AssetObjectFileViewModel>> api)
    {
        if (asset is null)
        {
            AssetFileObjects = [];
            return;
        }

        AssetFileObjects = api(new AssetRequestBody(asset.AssetId)) ?? [];
    }

    public void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) Category = EditorAssetCategory.None;
        AssetObjects.Clear();
        AssetFileObjects.Clear();
    }
}

public record AssetObjectViewModel(
    int AssetId,
    int ResourceId,
    string ResourceName,
    string Name,
    bool IsCoreAsset,
    int Generation,
    string SpecialName,
    string SpecialValue,
    bool HasActions);

public sealed record AssetObjectFileViewModel(
    int AssetFileId,
    string RelativePath,
    long SizeInBytes,
    string? ContentHash);