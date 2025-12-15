using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

public sealed class SceneWorld
{
    private readonly World _world;

    internal SceneWorld(World world)
    {
        _world = world;
    }
    
    
}