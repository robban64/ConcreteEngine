using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Scene;

public readonly record struct WorldEntityId(int Value)
{
    public static implicit operator int(WorldEntityId handle) => handle.Value;
}

public readonly record struct GameEntityId(int Value, ushort Gen) 
{
    public static implicit operator int(GameEntityId handle) => handle.Value;
}