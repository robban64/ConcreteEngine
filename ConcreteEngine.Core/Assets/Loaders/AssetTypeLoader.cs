#region

using ConcreteEngine.Core.Assets.Manifest;

#endregion

namespace ConcreteEngine.Core.Assets.Loaders;

internal interface IAssetTypeLoader;

internal abstract class AssetTypeLoader<TRecord, TPayload>(IReadOnlyList<TRecord> records)
    : IAssetTypeLoader where TRecord : class, IAssetManifestRecord
{
    private IReadOnlyList<TRecord> _records = records;
    private int _idx = 0;

    private readonly AssetLoadEntry<TRecord, TPayload> _loadEntry = new();

    public abstract TPayload ProcessResource(TRecord record, out AssetProcessInfo info);

    public bool ProcessNext(out AssetLoadEntry<TRecord, TPayload> data)
    {
        if (_idx >= _records.Count)
        {
            data = null!;
            return false;
        }

        var record = _records[_idx++];
        var payload = ProcessResource(record, out var status);
        _loadEntry.Payload = payload;
        _loadEntry.ProcessInfo = status;
        _loadEntry.Record = record;
        data = _loadEntry;
        return true;
    }

    public virtual void Prepare()
    {
        
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