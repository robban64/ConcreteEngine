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
    string RelativePath)
    : IComparable<AssetFile>
{
    public Guid GId { get; } = GId;
    public AssetFileId Id { get; } = Id;
    public FileBinding Binding { get; internal set; } = Binding;
    public AssetStorage Storage { get; internal set; } = Storage;
    public DateTime LastWriteTime { get; internal set; } = LastWriteTime;
    public long SizeBytes { get; internal set; } = SizeBytes;
    public string LogicalName { get; internal set; } = LogicalName;
    public string RelativePath { get; internal set; } = RelativePath;
    
    public bool IsRoot() => Binding == FileBinding.RootFile;

    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Id.CompareTo(other.Id.Id);
    }
}