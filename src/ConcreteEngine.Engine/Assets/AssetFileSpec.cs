using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets;

public sealed record AssetFileSpec(
    AssetFileId Id,
    Guid GId,
    AssetStorageKind Storage,
    string LogicalName,
    string RelativePath,
    long SizeBytes,
    DateTime LastWriteTime,
    string? ContentHash = null,
    string? Source = null);