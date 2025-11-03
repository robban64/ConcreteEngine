namespace Core.DebugTools.Data;

public sealed class AssetStoreViewModel
{
    public string AssetKind { get; set; }
    public List<AssetObjectViewModel> AssetObjects { get; } = new(16);
    
    public List<AssetObjectFileViewModel> AssetFileObjects { get; set; } = [];
}

public record AssetObjectViewModel(
    int AssetId,
    int GfxId,
    string Name,
    bool IsCoreAsset,
    int Generation,
    string AssetKind);

public sealed record AssetObjectFileViewModel(
    int AssetFileId,
    string RelativePath,
    long SizeInBytes,
    string? ContentHash);