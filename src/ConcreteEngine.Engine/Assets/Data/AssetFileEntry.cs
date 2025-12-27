using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Data;

public sealed record AssetFileEntry
{
    internal AssetFileEntry(AssetFileId id, AssetFileSpec spec)
    {
        Id = id;
        Storage = spec.Storage;
        LogicalName = spec.LogicalName;
        RelativePath = spec.RelativePath;
        SizeBytes = spec.SizeBytes;
        ContentHash = spec.ContentHash;
        Source = spec.Source;
    }

    public AssetFileId Id { get; }
    public AssetStorageKind Storage { get; }
    public string LogicalName { get; }
    public string RelativePath { get; }
    public long SizeBytes { get; }
    public string? ContentHash { get; }
    public string? Source { get; }
}

internal sealed class AssetFileSpec(
    AssetStorageKind storage,
    string logicalName,
    string relativePath,
    long sizeBytes,
    string? contentHash = null,
    string? source = null
)
{
    public AssetStorageKind Storage { get; } = storage;
    public string LogicalName { get; } = logicalName;
    public string RelativePath { get; } = relativePath;
    public long SizeBytes { get; } = sizeBytes;
    public string? ContentHash { get; } = contentHash;
    public string? Source { get; } = source;
}