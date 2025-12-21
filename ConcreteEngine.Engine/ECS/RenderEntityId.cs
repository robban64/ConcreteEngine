using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.ECS;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    public bool IsValid() => Id > 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    public static implicit operator int(RenderEntityId id) => id.Id;
}

