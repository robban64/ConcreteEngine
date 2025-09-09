namespace ConcreteEngine.Core.Assets.Loaders;



internal abstract class AssetTypeLoader<TRecord, TPayload>(IReadOnlyList<TRecord> records)
    where TRecord : class, IAssetManifestRecord
    where TPayload : struct, allows ref struct
{
    private IReadOnlyList<TRecord> _records = records;
    private int _idx = 0;
    
    public abstract TPayload Get(TRecord record);

    protected virtual void ClearCache()
    {
    }
    
    public bool ProcessNext(out TRecord? record, out TPayload data)
    {
        if (_idx >= _records.Count)
        {
            data = default;
            record = null;
            return false;
        }
        
        record = _records[_idx++];
        data = Get(record);
        return true;
    }

    public void Finish()
    {
        _records = null;
        ClearCache();
    }
}