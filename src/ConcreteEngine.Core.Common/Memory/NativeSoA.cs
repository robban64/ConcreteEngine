using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

// ReSharper disable OutParameterValueIsAlwaysDiscarded.Local
public unsafe struct NativeSoA<T1, T2> : IDisposable where T1 : unmanaged where T2 : unmanaged
{
    public static int StrideSum => Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>();

    public int Length { get; private set; }
    
    private T1* _ptr1;
    private T2* _ptr2;
    
    private NativeArray<byte> _array;
    
    public static NativeSoA<T1,T2> Allocate(int length, bool zeroed = true) => new (length, 0, zeroed);
    public static NativeSoA<T1,T2> AlignedAllocate(int length, int alignment, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);
        return new NativeSoA<T1, T2>(length, alignment, zeroed);
    }


    private NativeSoA(int length, int alignment, bool zeroed)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        var capacity = length * StrideSum;
        _array = alignment == 0
            ? NativeArray.Allocate(capacity, zeroed)
            : NativeArray.AlignedAllocate(IntMath.AlignUp(capacity, alignment), alignment, zeroed);

        var allocator = new NativeAllocBuilder(_array, alignCursor: alignment);
        _ptr1 = allocator.AllocRaw<T1>(length);
        _ptr2 = allocator.AllocRaw<T2>(length);
        Length = length;

    }

    public readonly int AllocatedSize => _array.Length;
    public readonly int Alignment => _array.Alignment;

    public readonly bool IsNull => _array.IsNull;
    public readonly bool IsNullOrEmpty => _array.IsNullOrEmpty;

    public readonly int SizeInBytes => Length * StrideSum;
    
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


    public readonly NativeView<T1> View1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, Length);
    }

    public readonly NativeView<T2> View2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, Length);
    }

    
    public readonly Span<T1> Span1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, Length);
    }

    public readonly Span<T2> Span2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, Length);
    }

    
    public void ReAlloc(int length, bool zeroed)
    {
        var capacity = length * StrideSum;
        _array.ReAlloc(capacity, zeroed);

        var allocator = new NativeAllocBuilder(_array);
        _ptr1 = allocator.AllocRaw<T1>(length);
        _ptr2 = allocator.AllocRaw<T2>(length);

        Length = length;
    }

    public void Dispose()
    {
        _array.Dispose();
        _ptr1 = null;
        _ptr2 = null;
        Length = 0;
    }

    public void Clear() => _array.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2> GetEnumerator() => new(View1, View2);
}

public unsafe struct NativeSoA<T1, T2, T3> : IDisposable where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
{
    public static int StrideSum => Unsafe.SizeOf<T1>() + Unsafe.SizeOf<T2>() + Unsafe.SizeOf<T3>();
    
    public static NativeSoA<T1,T2,T3> Allocate(int length, bool zeroed = true) => new (length, 0, zeroed);
    public static NativeSoA<T1,T2, T3> AlignedAllocate(int length, int alignment, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alignment);
        return new NativeSoA<T1, T2, T3>(length, alignment, zeroed);
    }


    public int Length { get; private set; }

    private T1* _ptr1;
    private T2* _ptr2;
    private T3* _ptr3;

    private NativeArray<byte> _array;

    public readonly bool IsNull => _ptr1 == null;
    public readonly bool IsNullOrEmpty => _ptr1 == null || Length <= 0;
    public readonly int SizeInBytes => Length * StrideSum;

    private NativeSoA(int length, int alignment = 0, bool zeroed = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        var capacity = length * StrideSum;
        _array = alignment == 0
            ? NativeArray.Allocate(capacity, zeroed)
            : NativeArray.AlignedAllocate(IntMath.AlignUp(capacity, alignment), alignment, zeroed);

        var allocator = new NativeAllocBuilder(_array, alignment);
        _ptr1 = allocator.AllocRaw<T1>(length);
        _ptr2 = allocator.AllocRaw<T2>(length);
        _ptr3 = allocator.AllocRaw<T3>(length);

        Length = length;
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


    public readonly NativeView<T1> View1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, Length);
    }

    public readonly NativeView<T2> View2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, Length);
    }

    public readonly NativeView<T3> View3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr3, Length);
    }
    public readonly Span<T1> Span1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr1, Length);
    }

    public readonly Span<T2> Span2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr2, Length);
    }

    public readonly Span<T3> Span3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_ptr3, Length);
    }

    

    public void ReAlloc(int length, bool zeroed)
    {
        var capacity = length * StrideSum;
        _array.ReAlloc(capacity, zeroed);

        var allocator = new NativeAllocBuilder(_array);
        _ptr1 = allocator.AllocRaw<T1>(length);
        _ptr2 = allocator.AllocRaw<T2>(length);
        _ptr3 = allocator.AllocRaw<T3>(length);

        Length = length;
    }

    public void Clear() => _array.Clear();

    public void Dispose()
    {
        _array.Dispose();
        _ptr1 = null;
        _ptr2 = null;
        _ptr3 = null;
        Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T1, T2, T3> GetEnumerator() => new(View1, View2, View3);
}