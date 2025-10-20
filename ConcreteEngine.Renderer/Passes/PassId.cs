namespace ConcreteEngine.Renderer.Passes;

public readonly record struct PassId(byte Value) : IComparable<PassId>
{
    public static implicit operator int(PassId id) => id.Value;
    public int CompareTo(PassId other) => Value.CompareTo(other.Value);
}