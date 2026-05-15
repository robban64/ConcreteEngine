using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.ECS;

public abstract class GameEntityTag;
public readonly record struct GameEntityId(int Id) : IComparable<GameEntityId>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);

    public static implicit operator Id32<GameEntityTag>(GameEntityId id) => new(id.Id);
    public static explicit operator GameEntityId(Id32<GameEntityTag> id) => new(id.Value);

}