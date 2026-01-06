using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

namespace ConcreteEngine.Engine.Assets.Loader;

internal ref struct LoaderContext(AssetId id)
{
    public List<EmbeddedRecord>? Embedded;
    public readonly AssetId Id = id;
}
