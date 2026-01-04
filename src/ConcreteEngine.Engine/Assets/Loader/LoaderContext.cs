using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class LoaderContext
{
    public AssetId TargetId;
    public AssetRecord Record;
    public string FilePath;
    public bool IsHotReload;

    public List<EmbeddedRecord>? Embedded = null;

    public void AddEmbedded<TEmbedded>(TEmbedded record) where TEmbedded : EmbeddedRecord
    {
        Embedded ??= new List<EmbeddedRecord>(8);
        Embedded.Add(record);
    }

    public T GetTypedRecord<T>() where T : AssetRecord => (T)Record;
}

internal abstract class LoaderStorage 
{
    public abstract long TotalSize { get; }
    public abstract void Teardown();
}