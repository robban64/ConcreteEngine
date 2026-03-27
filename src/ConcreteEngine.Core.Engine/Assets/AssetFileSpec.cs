namespace ConcreteEngine.Core.Engine.Assets;

public sealed record AssetFileSpec(
    AssetFileId Id,
    Guid GId,
    AssetStorageKind Storage,
    string LogicalName,
    string RelativePath,
    long SizeBytes,
    DateTime LastWriteTime,
    string? ContentHash = null,
    string? Source = null) : IComparable<AssetFileSpec>
{
    public int CompareTo(AssetFileSpec? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);

    }
}