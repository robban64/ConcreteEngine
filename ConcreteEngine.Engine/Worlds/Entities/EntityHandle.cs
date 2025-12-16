using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Entities;

public readonly record struct EntityHandle(int Value) : IComparable<EntityHandle>
{
    public readonly int Value = Value;

    public bool IsValid => Value > 0;

    public int CompareTo(EntityHandle other) => Value.CompareTo(other.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(EntityHandle handle) => handle.Value;
}

public readonly record struct EntityId(int Handle, ushort Gen, EntityKind Kind) : IComparable<EntityId>
{
    public readonly int Handle = Handle;
    public readonly ushort Gen = Gen;
    public readonly EntityKind Kind = Kind;

    public bool IsValid => Handle > 0 && Kind != EntityKind.Unknown;

    public int CompareTo(EntityId other) => Handle.CompareTo(other.Handle);

    public static implicit operator int(EntityId handle) => handle.Handle;
    public static implicit operator EntityHandle(EntityId handle) => new(handle.Handle);
}