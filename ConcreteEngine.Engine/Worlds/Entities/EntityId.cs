using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Entities;

public readonly record struct EntityId(int Value) : IComparable<EntityId>
{
    public readonly int Value = Value;

    public bool IsValid => Value > 0;

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(EntityId id) => id.Value;
}

