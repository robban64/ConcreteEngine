using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets.Loaders;

internal interface IAssetLoadEntry
{
    public AssetProcessInfo Info { get; }
}
internal sealed class AssetLoadEntry<TRecord, TPayload> : IAssetLoadEntry
    where TRecord : class, IAssetManifestRecord 
{
    public AssetProcessInfo Info { get; set; }
    public required TRecord Record { get; set; }
    public TPayload Payload { get; set; }
}

public enum AssetProcessStatus
{
    Done,
    Repeat,
    Failed
}

internal readonly record struct AssetProcessInfo(
    AssetProcessStatus Status,
    AssetKind AssetType
)
{
    public static AssetProcessInfo MakeDone<TRecord>() where TRecord : class, IAssetManifestRecord
        => new(AssetProcessStatus.Done, TRecord.Kind);
    
    public static AssetProcessInfo MakeRepeat<TRecord>() where TRecord : class, IAssetManifestRecord
        => new(AssetProcessStatus.Repeat, TRecord.Kind);
    
    public static AssetProcessInfo MakeFailed<TRecord>() where TRecord : class, IAssetManifestRecord
        => new(AssetProcessStatus.Failed, TRecord.Kind);


}

internal interface IAssetTypeLoader;
internal abstract class AssetTypeLoader<TRecord, TPayload>(IReadOnlyList<TRecord> records) 
    : IAssetTypeLoader where TRecord : class, IAssetManifestRecord
{
    private IReadOnlyList<TRecord> _records = records;
    private int _idx = 0;

    private AssetLoadEntry<TRecord, TPayload> LoadEntry = new();
    
    public abstract TPayload ProcessResource(TRecord record, out AssetProcessInfo info);

    public bool ProcessNext(out AssetLoadEntry<TRecord,TPayload> data)
    {
        if (_idx >= _records.Count)
        {
            data = null!;
            return false;
        }

        var record = _records[_idx++];
        var payload = ProcessResource(record, out var status);
        LoadEntry.Payload = payload;
        LoadEntry.Info = status;
        LoadEntry.Record = record;
        data = LoadEntry;
        return true;
    }
    
    protected virtual void ClearCache()
    {
    }

    public void Finish()
    {
        _records = null!;
        ClearCache();
    }
}