using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal ref struct LoaderContext(AssetId id)
{
    public readonly AssetId Id = id;
}