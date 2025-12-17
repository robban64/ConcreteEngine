using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Scene;

public readonly record struct GameEntityId(int Value, ushort Gen)
{
    public readonly int Value = Value;
    public readonly ushort Gen = Gen;
    public static implicit operator int(GameEntityId handle) => handle.Value;
}

public readonly record struct GameEntityHandle(int Value, EntityId Entity, ushort Gen) : IComparable<GameEntityHandle>
{
    public readonly int Value = Value;
    public readonly EntityId Entity = Entity;
    public readonly ushort Gen = Gen;

    public bool IsValid => Value > 0 && Entity > 0;

    public int CompareTo(GameEntityHandle other) => Value.CompareTo(other.Value);

    public static implicit operator int(GameEntityHandle handle) => handle.Value;
    public static implicit operator EntityId(GameEntityHandle handle) => handle.Entity;
}