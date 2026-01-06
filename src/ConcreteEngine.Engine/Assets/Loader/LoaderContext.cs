using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Loader;

internal ref struct LoaderContext(AssetId id, string filePath)
{
    public List<EmbeddedRecord>? Embedded;
    public readonly string FilePath = filePath;
    public readonly AssetId Id = id;

    public void AddEmbedded<TEmbedded>(TEmbedded record) where TEmbedded : EmbeddedRecord
    {
        Embedded ??= new List<EmbeddedRecord>(8);
        Embedded.Add(record);
    }
}