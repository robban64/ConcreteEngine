namespace ConcreteEngine.Engine.Worlds.Data;

public readonly record struct ModelId(int Value)
{
    public static implicit operator int(ModelId id) => id.Value;
}