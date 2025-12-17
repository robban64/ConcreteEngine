using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Scene;

public readonly record struct SceneObjectId(int Value, ushort Gen) 
{
    public static implicit operator int(SceneObjectId handle) => handle.Value;
}