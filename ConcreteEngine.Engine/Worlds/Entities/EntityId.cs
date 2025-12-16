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

public readonly record struct WorldEntityId(int Value, int Slot, ushort Gen) : IComparable<WorldEntityId>
{
    public readonly int Value = Value;
    public readonly int Slot = Slot;
    public readonly ushort Gen = Gen;

    public bool IsValid => Value > 0;

    public int CompareTo(WorldEntityId other) => Value.CompareTo(other.Value);

    public static implicit operator int(WorldEntityId handle) => handle.Value;
    public static implicit operator EntityId(WorldEntityId handle) => new(handle.Slot);
}