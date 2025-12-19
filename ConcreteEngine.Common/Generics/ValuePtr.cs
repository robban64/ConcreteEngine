using System.Runtime.CompilerServices;

namespace ConcreteEngine.Common.Generics;

public readonly ref struct ValuePtr<T>(ref T value)
{
    private readonly ref T _value = ref value;

    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(in _value);
    }

    public ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }
    
    public static ValuePtr<T> Null
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(ref Unsafe.NullRef<T>());
    }
    
    public static implicit operator bool(ValuePtr<T> ptr) => !Unsafe.IsNullRef(in ptr._value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNullRef() => throw new NullReferenceException("ValuePtr is null");
}