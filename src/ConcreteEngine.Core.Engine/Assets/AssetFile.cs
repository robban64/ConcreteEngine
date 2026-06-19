namespace ConcreteEngine.Core.Engine.Assets;

public enum FileBinding : byte
{
    Unknown,
    RootFile,
    DependentFile,
    UnboundFile
}

public sealed record AssetFile(
    Guid GId,
    AssetFileId Id,
    FileBinding Binding,
    AssetStorage Storage,
    DateTime LastWriteTime,
    long SizeBytes,
    string LogicalName,
    string RelativePath
) : IComparable<AssetFile>
{
    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Id.CompareTo(other.Id.Id);
    }
}