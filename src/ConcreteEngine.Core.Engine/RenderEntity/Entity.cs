using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator int(RenderEntityId e) => e.Id;
}
