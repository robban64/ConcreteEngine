#region

using ConcreteEngine.Engine.Worlds;

#endregion

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