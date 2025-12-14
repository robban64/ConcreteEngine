using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene.Editor;

internal sealed class EditorObjectSelector
{
    private readonly World _world;

    public EditorObjectSelector(World world)
    {
        _world = world;
    }

    public void Update(float dt)
    {
    }
}