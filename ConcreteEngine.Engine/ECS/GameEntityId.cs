using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.ECS;

public readonly record struct GameEntityId(int Id, ushort Gen) : IComparable<GameEntityId>
{
    public readonly int Id = Id;
    public readonly ushort Gen = Gen;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Id > 0 && Gen > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Id - 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);
    
    public static implicit operator int(GameEntityId e) => e.Id;
}