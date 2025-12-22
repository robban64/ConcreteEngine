using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Identity;

public readonly record struct Handle(int Value, ushort Gen=0) : IComparable<Handle>
{
    public int Index() => Value - 1;
    public int CompareTo(Handle other) => Value.CompareTo(other.Value);
    public static implicit operator int(Handle id) => id.Value;
}

public readonly record struct Handle<T>(int Value, ushort Gen=0) : IComparable<Handle<T>>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Index() => Value - 1;
    public int CompareTo(Handle<T> other) => Value.CompareTo(other.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Handle<T> id) => id.Value;
}

