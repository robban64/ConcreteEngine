using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.ECS;

public readonly record struct RenderEntityId(int Id) : IComparable<RenderEntityId>
{
    public readonly int Id = Id;

    public bool IsValid => Id > 0;
    
    public int Index => Id - 1;

    public int CompareTo(RenderEntityId other) => Id.CompareTo(other.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(RenderEntityId id) => id.Id;
}

