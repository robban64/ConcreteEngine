namespace ConcreteEngine.Graphics.Handles;

public readonly record struct UboSlot(uint Value) : IComparable<UboSlot>
{
    public static implicit operator uint(UboSlot slot) => slot.Value;

    public int CompareTo(UboSlot other) => Value.CompareTo(other.Value);
}