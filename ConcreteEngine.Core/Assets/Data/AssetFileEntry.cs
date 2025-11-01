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

internal sealed record AssetFileSpec(
    AssetStorageKind Storage,
    string LogicalName,
    string RelativePath,
    long SizeBytes,
    string? ContentHash = null,
    string? Source = null
);