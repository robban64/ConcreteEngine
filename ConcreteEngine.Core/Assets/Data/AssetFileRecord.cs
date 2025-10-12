namespace ConcreteEngine.Core.Assets;

public sealed record AssetFileRecord(
    AssetFileId Id,
    string LogicalName,
    string RelativePath,
    AssetStorageKind Storage,
    long SizeBytes,
    string? ContentHash,
    string? Source
);