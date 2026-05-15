using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Core.Engine.ECS;

public abstract class RenderEntityTag;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    public static implicit operator Id32<RenderEntityTag>(RenderEntityId id) => new(id.Id);
    public static explicit operator RenderEntityId(Id32<RenderEntityTag> id) => new(id.Value);

}