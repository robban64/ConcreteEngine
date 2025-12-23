using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Common.Memory.Enumerators;

public ref struct RefEnumerator<T> where T : unmanaged
{
    private readonly ref T _start;
    private readonly int _length;
    private int _i;

    public RefEnumerator(Span<T> span)
    {
        _start = ref MemoryMarshal.GetReference(span);
        _length = span.Length;
        _i = -1;
    }

    public bool MoveNext() => ++_i < _length;
    public ref T Current => ref Unsafe.Add(ref _start, _i);
}