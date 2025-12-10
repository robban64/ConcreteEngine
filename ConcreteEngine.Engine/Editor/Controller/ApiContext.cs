#region

using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics;
using ConcreteEngine.Renderer.State;

#endregion

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