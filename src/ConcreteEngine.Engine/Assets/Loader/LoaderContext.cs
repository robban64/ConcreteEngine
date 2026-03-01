using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader;

internal readonly struct LoaderContext(AssetId id)
{
    public readonly AssetId Id = id;
}