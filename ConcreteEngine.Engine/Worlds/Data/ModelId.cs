namespace ConcreteEngine.Engine.Worlds.Data;

public readonly record struct ModelId(int Value)
{
    public static ModelId Ignore => new(-1);
    public static implicit operator int(ModelId id) => id.Value;
}