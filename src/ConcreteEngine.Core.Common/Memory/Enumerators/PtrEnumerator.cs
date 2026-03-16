using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory.Enumerators;

public unsafe struct PtrEnumerator<T> where T : unmanaged
{
    private readonly T* _ptr;
    private readonly int _length;
    private int _i;

    internal PtrEnumerator(T* ptr, int length)
    {
        _ptr = ptr;
        _length = length;
        _i = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _length;

    public readonly ref T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _ptr[_i];
    }
}