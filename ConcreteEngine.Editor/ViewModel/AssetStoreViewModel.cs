using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.ViewModel;


public sealed class AssetStoreViewModel
{
    public EditorAssetSelection TypeSelection { get; set; }
    public List<AssetObjectViewModel> AssetObjects { get; set; } = [];
    public List<AssetObjectFileViewModel> AssetFileObjects { get; set; } = [];

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