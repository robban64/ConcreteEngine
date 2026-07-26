namespace ConcreteEngine.Graphics.Handles;

public readonly record struct UboSlot(byte Value) : IComparable<UboSlot>
{
    public static implicit operator UboSlot(byte b) => new(b);

    public static implicit operator uint(UboSlot slot) => slot.Value;

    public int CompareTo(UboSlot other) => Value.CompareTo(other.Value);
}