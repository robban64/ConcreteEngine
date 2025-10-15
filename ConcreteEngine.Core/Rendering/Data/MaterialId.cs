namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialId(int Id)
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);
}
