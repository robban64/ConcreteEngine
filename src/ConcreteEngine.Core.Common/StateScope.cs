using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common;

public readonly ref struct StateScope<T>(ref T value, ref bool isDirty) where T : unmanaged
{
    private readonly ref T _value = ref value;
    private readonly ref bool _isDirty = ref isDirty;

    public ref readonly T Value => ref _value;

    public ref T Mutate
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            _isDirty = true;
            return ref _value;
        }
    }
}