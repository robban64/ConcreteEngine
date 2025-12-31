namespace ConcreteEngine.Engine.Worlds.Data;

public readonly record struct EmitterHandle : IComparable<EmitterHandle>
{
    public readonly ushort Value;
    public readonly ushort Gen;

    public int Index() => Value - 1;

    public EmitterHandle(int value, int gen)
    {
        Value = (ushort)value;
        Gen = (ushort)gen;
    }

    public EmitterHandle(ushort value, ushort gen)
    {
        Value = value;
        Gen = gen;
    }

    public int CompareTo(EmitterHandle other) => Value.CompareTo(other.Value);


    public static implicit operator ushort(EmitterHandle handle) => handle.Value;
}