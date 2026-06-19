namespace ConcreteEngine.Core.Engine.Assets;

public enum FileBinding : byte
{
    Unknown,
    RootFile,
    DependentFile,
    UnboundFile
}

public sealed record AssetFile(
    AssetFileId Id,
    Guid GId,
    FileBinding Binding,
    AssetStorage Storage,
    DateTime LastWriteTime,
    long SizeBytes,
    string LogicalName,
    string RelativePath,
    string? Source = null,
    string? ContentHash = null
) : IComparable<AssetFile>
{
    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Value.CompareTo(other.Id.Value);
    }
}