
namespace ConcreteEngine.Core.Assets.Data;

public sealed record AssetFileEntry
{
    internal AssetFileEntry(AssetFileId id, in AssetFileSpec spec)
    {
        Id = id;
        Storage = spec.Storage;
        LogicalName = spec.LogicalName;
        RelativePath = spec.RelativePath;
        SizeBytes = spec.SizeBytes;
        ContentHash = spec.ContentHash;
        Source = spec.Source;
    }

    public AssetFileId Id { get; init; }
    public AssetStorageKind Storage { get; init; }
    public string LogicalName { get; init; }
    public string RelativePath { get; init; }
    public long SizeBytes { get; init; }
    public string? ContentHash { get; init; }
    public string? Source { get; init; }
}

internal readonly struct AssetFileSpec(
    AssetStorageKind storage,
    string logicalName,
    string relativePath,
    long sizeBytes,
    string? contentHash = null,
    string? source = null
)
{
    public long SizeBytes { get; } = sizeBytes;
    public string LogicalName { get; } = logicalName;
    public string RelativePath { get; } = relativePath;
    public string? ContentHash { get; } = contentHash;
    public string? Source { get; } = source;
    public AssetStorageKind Storage { get; } = storage;

}