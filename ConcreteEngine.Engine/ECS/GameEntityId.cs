namespace ConcreteEngine.Engine.ECS;

public readonly record struct GameEntityId(int Id, ushort Gen) : IComparable<GameEntityId>
{
    public readonly int Id = Id;
    public readonly ushort Gen = Gen;

    public int Index => Id - 1;
    
    public int CompareTo(GameEntityId other) => Id.CompareTo(other.Id);
    
    public static implicit operator int(GameEntityId e) => e.Id;
}