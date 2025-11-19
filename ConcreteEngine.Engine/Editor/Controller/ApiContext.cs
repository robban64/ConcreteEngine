using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext
{
    public World World { get; }
    public AssetSystem AssetSystem { get; }


    public ApiContext(World world, AssetSystem assetSystem)
    {
        World = world;
        AssetSystem = assetSystem;
    }
}