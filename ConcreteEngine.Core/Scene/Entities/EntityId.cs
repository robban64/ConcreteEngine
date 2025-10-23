namespace ConcreteEngine.Core.Scene.Entities;

//ushort Gen, EntityId.DomainKind Kind
public readonly record struct EntityId(int Id) : IComparable<EntityId>
{
    public int CompareTo(EntityId other) => Id.CompareTo(other.Id);

    public static implicit operator int(EntityId id) => id.Id;
    public static explicit operator EntityId(int value) => new(value);

    /*
    public static EntityId MakeMeshId(int id) => new (id, 0, DomainKind.Mesh);
    public static EntityId MakeTerrainId(int id) => new (id, 0, DomainKind.Terrain);

    public enum DomainKind : byte
    {
        Invalid = 0,
        Skybox = 1,
        Mesh = 2,
        Terrain = 3
    }
*/
}