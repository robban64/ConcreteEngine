#region

using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Data;

public sealed class AssetStoreViewModel
{
    public EditorAssetSelection TypeSelection { get; set; }
    public List<AssetObjectViewModel> AssetObjects { get; } = new(16);
    public List<AssetObjectFileViewModel> AssetFileObjects { get; } = new(4);

    public void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) TypeSelection = EditorAssetSelection.None;
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