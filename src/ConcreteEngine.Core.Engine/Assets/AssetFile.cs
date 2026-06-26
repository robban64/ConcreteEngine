using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Engine.Assets;

public enum FileBinding : byte
{
    Unknown, RootFile, DependentFile, UnboundFile
}

public sealed class AssetFile(
    Guid gId,
    AssetFileId id,
    FileBinding binding,
    AssetStorage storage,
    DateTime lastWriteTime,
    long sizeBytes,
    string logicalName,
    string relativePath) : IComparable<AssetFile>
{
    public Guid GId { get; } = gId;

    [JsonIgnore]
    public AssetFileId Id { get; } = id;

    public FileBinding Binding { get; internal set; } = binding;
    public AssetStorage Storage { get; internal set; } = storage;
    public DateTime LastWriteTime { get; internal set; } = lastWriteTime;
    public long SizeBytes { get; internal set; } = sizeBytes;
    public string LogicalName { get; internal set; } = logicalName;
    public string RelativePath { get; internal set; } = relativePath;

    public bool IsRoot() => Binding == FileBinding.RootFile;

    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.Id.CompareTo(other.Id.Id);
    }
}