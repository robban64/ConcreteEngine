namespace ConcreteEngine.Core.Engine.Assets;

public enum FileSpecBinding : byte
{
    UnboundFile,
    DependentFile,
    RootFile
}

public sealed record AssetFileSpec(
    AssetFileId Id,
    Guid GId,
    AssetStorageKind Storage,
    DateTime LastWriteTime,
    long SizeBytes,
    string LogicalName,
    string RelativePath,
    string? ContentHash = null,
    string? Source = null) : IComparable<AssetFileSpec>
{
    public int CompareTo(AssetFileSpec? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);

    }
}