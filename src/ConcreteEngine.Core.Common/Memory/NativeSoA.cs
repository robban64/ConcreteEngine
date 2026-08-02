namespace ConcreteEngine.Core.Common.Memory;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable OutParameterValueIsAlwaysDiscarded.Local
public unsafe struct NativeSoA<T1, T2> : IDisposable where T1 : unmanaged where T2 : unmanaged
{
    private T1* _ptr1;
    private T2* _ptr2;

    public int Length;

    public NativeSoA(int length, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        var capacity = GetSizeInBytes(length, out var sizeT1, out _);
        var array = NativeArray.Allocate(capacity, zeroed);

        Length = length;
        _ptr1 = (T1*)array.Ptr;
        _ptr2 = (T2*)(array.Ptr + sizeT1);
    }

    public readonly bool IsNull => _ptr1 == null;

    public readonly NativeView<T1> View1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, 0, Length);
    }

    public readonly NativeView<T2> View2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, 0, Length);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T1 At1(int index) => ref _ptr1[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T2 At2(int index) => ref _ptr2[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Set(int index, T1 t1, T2 t2)
    {
        _ptr1[index] = t1;
        _ptr2[index] = t2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2> GetEnumerator() => new(_ptr1, _ptr2, Length);

    public void ReAlloc(int length, bool zeroed)
    {
        int sizeT1 = length * Unsafe.SizeOf<T1>(), sizeT2 = length * Unsafe.SizeOf<T2>();

        var array = new NativeArray<byte>((byte*)_ptr1, sizeT1 + sizeT2, 0);
        array.ReAlloc(sizeT1 + sizeT2, zeroed);

        Length = length;
        _ptr1 = (T1*)array.Ptr;
        _ptr2 = (T2*)(array.Ptr + sizeT1);
    }

    public void Dispose()
    {
        NativeArray.DisposeArray(_ptr1, GetSizeInBytes(Length, out _, out _), 0);
        _ptr1 = null;
        _ptr2 = null;
    }

    private static int GetSizeInBytes(int len, out int sizeT1, out int sizeT2)
    {
        sizeT1 = len * Unsafe.SizeOf<T1>();
        sizeT2 = len * Unsafe.SizeOf<T2>();
        return sizeT1 + sizeT2;
    }
}

public unsafe struct NativeSoA<T1, T2, T3> : IDisposable where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    private T1* _ptr1;
    private T2* _ptr2;
    private T3* _ptr3;

    public int Length;

    public NativeSoA(int length, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);
        var capacity = GetSizeInBytes(length, out var sizeT1, out var sizeT2, out _);
        var bytes = NativeArray.Allocate(capacity, zeroed);

        Length = length;
        _ptr1 = (T1*)bytes.Ptr;
        _ptr2 = (T2*)(bytes.Ptr + sizeT1);
        _ptr3 = (T3*)((byte*)_ptr2 + sizeT2);
    }

    public readonly bool IsNull => _ptr1 == null;

    public readonly NativeView<T1> View1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, 0, Length);
    }

    public readonly NativeView<T2> View2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, 0, Length);
    }

    public readonly NativeView<T3> View3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr3, 0, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T1 At1(int index) => ref _ptr1[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T2 At2(int index) => ref _ptr2[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T3 At3(int index) => ref _ptr3[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Set(int index, T1 t1, T2 t2, T3 t3)
    {
        _ptr1[index] = t1;
        _ptr2[index] = t2;
        _ptr3[index] = t3;
    }

    public void ReAlloc(int length, bool zeroed)
    {
        var capacity = GetSizeInBytes(length, out var sizeT1, out var sizeT2, out _);
        var array = new NativeArray<byte>((byte*)_ptr1, capacity, 0);
        array.ReAlloc(sizeT1 + sizeT2, zeroed);

        Length = length;
        _ptr1 = (T1*)array.Ptr;
        _ptr2 = (T2*)(array.Ptr + sizeT1);
        _ptr3 = (T3*)((byte*)_ptr2 + sizeT2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2, T3> GetEnumerator() => new(_ptr1, _ptr2, _ptr3, Length);


    public void Dispose()
    {
        NativeArray.DisposeArray(_ptr1, GetSizeInBytes(Length, out _, out _, out _), 0);
        _ptr1 = null;
        _ptr2 = null;
        _ptr3 = null;
    }

    private static int GetSizeInBytes(int len, out int sizeT1, out int sizeT2, out int sizeT3)
    {
        sizeT1 = len * Unsafe.SizeOf<T1>();
        sizeT2 = len * Unsafe.SizeOf<T2>();
        sizeT3 = len * Unsafe.SizeOf<T3>();
        return sizeT1 + sizeT2 + sizeT3;
    }
}