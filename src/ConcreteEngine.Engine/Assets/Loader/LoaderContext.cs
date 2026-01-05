using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class LoaderContext
{
    public List<EmbeddedRecord>? Embedded = null;
    public string FilePath;
    public Guid GId;
    public AssetId Id;
    public bool IsHotReload;

    public void AddEmbedded<TEmbedded>(TEmbedded record) where TEmbedded : EmbeddedRecord
    {
        Embedded ??= new List<EmbeddedRecord>(8);
        Embedded.Add(record);
    }

}

internal abstract class LoaderStorage 
{
    public abstract long TotalSize { get; }
    public abstract void Teardown();
}