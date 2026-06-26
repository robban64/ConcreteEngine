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
    internal static AssetFile MakeRoot(AssetFileId id, AssetId assetRootId, string assetName, Guid assetGuid,
        in FileScanInfo scanInfo)
    {
        return new AssetFile(
            gId: assetGuid,
            id: id,
            assetRootId: assetRootId,
            isUnbound: false,
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
            isUnbound: isUnbound,
            storage: scanInfo.Storage,
            lastWriteTime: scanInfo.LastWriteTime,
            sizeBytes: scanInfo.SizeBytes,
            logicalName: scanInfo.Name,
            relativePath: scanInfo.RelativePath);
    }

    private AssetFile(
        Guid gId,
        AssetFileId id,
        AssetId assetRootId,
        bool isUnbound,
        AssetStorage storage,
        DateTime lastWriteTime,
        long sizeBytes,
        string logicalName,
        string relativePath)
    {
        if(!id.IsValid()) Throwers.InvalidArgument(nameof(id));
        if(assetRootId.IsValid() && isUnbound) Throwers.InvalidArgument(nameof(isUnbound));
        if(assetRootId.IsValid() && gId == Guid.Empty) Throwers.InvalidArgument(nameof(gId));

        GId = gId;
        Id = id;
        AssetRootId = assetRootId;
        IsUnbound = isUnbound;
        Storage = storage;
        LastWriteTime = lastWriteTime;
        SizeBytes = sizeBytes;
        LogicalName = logicalName;
        RelativePath = relativePath;
        
    }

    public Guid GId { get; }

    [JsonIgnore]
    public AssetFileId Id { get; }

    [JsonIgnore]
    public AssetId AssetRootId { get; }

    public bool IsUnbound { get; internal set; }

    public AssetStorage Storage { get; internal set; }
    public DateTime LastWriteTime { get; internal set; }
    public long SizeBytes { get; internal set; }
    public string LogicalName { get; internal set; }
    public string RelativePath { get; internal set; }

    public FileBinding GetBinding()
    {
        return AssetRootId.IsValid() ? FileBinding.RootFile : IsUnbound ? FileBinding.UnboundFile : FileBinding.DependentFile;

    }

    public int CompareTo(AssetFile? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Id.CompareTo(other.Id);
    }
}