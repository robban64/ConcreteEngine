namespace ConcreteEngine.Graphics.Resources;

public readonly record struct UboSlot(int Value) : IComparable<UboSlot>
{
    public static implicit operator int(UboSlot slot) => slot.Value;
    public static implicit operator uint(UboSlot slot) => (uint)slot.Value;

    public int CompareTo(UboSlot other) => Value.CompareTo(other.Value);
}