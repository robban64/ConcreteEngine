using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public ref struct OutPtr<T>(ref T value) where T : unmanaged
{
    private readonly ref T _value = ref value;
    private bool _hasSkipped;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValuePtr<T> FillValue()
    {
        Debug.Assert(!_hasSkipped);
        return new ValuePtr<T>(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FillNull(out T value)
    {
        Unsafe.SkipInit(out value);
        _hasSkipped = true;
    }

    private static void ThrowNullRef() => throw new NullReferenceException("FillPtr is null");
}