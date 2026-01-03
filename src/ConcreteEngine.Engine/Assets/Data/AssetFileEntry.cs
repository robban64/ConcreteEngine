using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Data;



public sealed record AssetFileSpec(
    AssetFileId Id,
    Guid GId,
    AssetStorageKind Storage,
    string LogicalName,
    string RelativePath,
    long SizeBytes,
    string? ContentHash = null,
    string? Source = null);
    