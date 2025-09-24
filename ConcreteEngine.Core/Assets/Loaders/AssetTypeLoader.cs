using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Assets.Loaders;

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
        LoadEntry.ProcessInfo = status;
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