namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialId(int Id) : IComparable<MaterialId>
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);

    public int CompareTo(MaterialId other) => Id.CompareTo(other.Id);
}