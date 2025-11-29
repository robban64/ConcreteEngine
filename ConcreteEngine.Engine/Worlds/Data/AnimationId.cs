namespace ConcreteEngine.Engine.Worlds.Data;

public readonly record struct AnimationId(int Value)
{
    public static implicit operator int(AnimationId id) => id.Value;
}