using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.ECS;

public sealed class RenderEntity;
public sealed class GameEntity;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    public static implicit operator Id32<RenderEntity>(RenderEntityId id) => new(id.Id);
    public static explicit operator RenderEntityId(Id32<RenderEntity> id) => new(id.Value);

    public static explicit operator int(RenderEntityId e) => e.Id;

}

public readonly record struct GameEntityId(int Id) : IComparable<GameEntityId>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);

    public static implicit operator Id32<GameEntity>(GameEntityId id) => new(id.Id);
    public static explicit operator GameEntityId(Id32<GameEntity> id) => new(id.Value);
    
    public static explicit operator int(GameEntityId e) => e.Id;

}