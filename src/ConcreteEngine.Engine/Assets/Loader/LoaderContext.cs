using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;

namespace ConcreteEngine.Engine.Assets.Loader;

internal ref struct LoaderContext(AssetId id)
{
    public List<EmbeddedRecord>? Embedded;
    public readonly AssetId Id = id;
}
