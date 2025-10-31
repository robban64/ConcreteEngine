namespace ConcreteEngine.Core.Worlds.Entities;

public readonly record struct EntityId(int Id) : IComparable<EntityId>
{
    public int CompareTo(EntityId other) => Id.CompareTo(other.Id);

    public static implicit operator int(EntityId id) => id.Id;
    public static explicit operator EntityId(int value) => new(value);
}