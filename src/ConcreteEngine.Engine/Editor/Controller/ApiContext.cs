using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext(World world, AssetSystem assetSystem, SceneWorld scene)
{
    public readonly World World = world;
    public readonly AssetSystem AssetSystem = assetSystem;
    public readonly SceneWorld Scene = scene;
}