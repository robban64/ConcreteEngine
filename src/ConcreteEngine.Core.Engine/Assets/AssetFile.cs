using System.Text.Json.Serialization;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;

namespace ConcreteEngine.Core.Engine.Assets;

public enum FileBinding : byte
{
    Unknown, RootFile, DependentFile, UnboundFile
}

public sealed class AssetFile : IComparable<AssetFile>
{
    public Guid GId { get; }

    [JsonIgnore]
    public AssetFileId Id { get; }

    [JsonIgnore]
    public AssetId AssetRootId { get; }

    public string LogicalName { get; internal set; }
    public string RelativePath { get; }

    public FileBinding Binding { get; private set; }
    public AssetStorage Storage { get; }
    public DateTime LastWriteTime { get; internal set; }
    public long SizeBytes { get; internal set; }
    
    private AssetFile(
        Guid gId,
        AssetFileId id,
        AssetId assetRootId,
        FileBinding binding,
        AssetStorage storage,
        string logicalName,
        string relativePath,
        long sizeBytes,
        DateTime lastWriteTime
    )
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id.IsValid(), false);
        ArgumentOutOfRangeException.ThrowIfZero((int)binding, nameof(binding));
        ArgumentOutOfRangeException.ThrowIfZero((int)storage, nameof(storage));

        if(assetRootId.IsValid() && gId == Guid.Empty) Throwers.InvalidArgument(nameof(gId));
        if(assetRootId.IsValid() && binding != FileBinding.RootFile) Throwers.InvalidArgument(nameof(binding));

        GId = gId;
        Id = id;
        AssetRootId = assetRootId;
        Binding = binding;
        Storage = storage;
        LastWriteTime = lastWriteTime;
        SizeBytes = sizeBytes;
        LogicalName = logicalName;
        RelativePath = relativePath;
        
    }

    public void MakeDependent()
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual((int)Binding, (int)FileBinding.UnboundFile);
        Binding = FileBinding.DependentFile;
    }

    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }
    
    //
    
    internal static AssetFile MakeRoot(AssetFileId id, AssetId assetRootId, string assetName, Guid assetGuid,
        in FileScanInfo scanInfo)
    {
        return new AssetFile(
            gId: assetGuid,
            id: id,
            assetRootId: assetRootId,
            binding: FileBinding.RootFile,
            storage: scanInfo.Storage,
            lastWriteTime: scanInfo.LastWriteTime,
            sizeBytes: scanInfo.SizeBytes,
            logicalName: assetName,
            relativePath: scanInfo.RelativePath);
    }
    
    internal static AssetFile MakeFile(AssetFileId id, bool isUnbound, in FileScanInfo scanInfo)
    {
        return new AssetFile(
            gId: Guid.NewGuid(),
            id: id,
            assetRootId: AssetId.Empty,
            binding: isUnbound ? FileBinding.UnboundFile : FileBinding.DependentFile,
            storage: scanInfo.Storage,
            lastWriteTime: scanInfo.LastWriteTime,
            sizeBytes: scanInfo.SizeBytes,
            logicalName: scanInfo.Name,
            relativePath: scanInfo.RelativePath);
    }

}