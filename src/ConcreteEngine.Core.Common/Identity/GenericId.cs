namespace ConcreteEngine.Core.Common.Identity;

public record struct Id32<T>(int Value) where T : class
{
    public static implicit operator int(Id32<T> id) => id.Value;
}

public readonly record struct IdGen32<T>(int Value, ushort Gen = 0) where T : class
{
    public int Index() => Value - 1;
    public static implicit operator int(IdGen32<T> id) => id.Value;
}