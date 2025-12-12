#region

using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext(World world, AssetSystem assetSystem)
{
    public readonly World World = world;
    public readonly AssetSystem AssetSystem = assetSystem;
}