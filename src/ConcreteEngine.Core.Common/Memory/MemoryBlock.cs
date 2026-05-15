using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Memory;

public readonly unsafe struct MemoryBlockPtr(MemoryBlock* ptr) : IEquatable<MemoryBlockPtr>
{
    public static int BlockSize => MemoryBlock.BlockSize;

    public bool IsNull => Ptr == null;

    public readonly MemoryBlock* Ptr = ptr;

    public int Cursor => Ptr->Cursor;
    public int Length => Ptr->Length;
    public int Remaining => Ptr->Remaining;

    public MemoryBlockPtr Next => new(Ptr->Next);

    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr->Data;
    }

    public void ResetCursor() => Ptr->ResetCursor();

    [UnscopedRef]
    public NativeAllocator GetAllocator(int alignment = 0) => Ptr->GetAllocator(alignment);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryBlockPtr(MemoryBlock* ptr) => new(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MemoryBlockPtr(IntPtr ptr) => new((MemoryBlock*)ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator IntPtr(MemoryBlockPtr ptr) => (IntPtr)ptr.Ptr;

    public static bool operator ==(MemoryBlockPtr left, MemoryBlockPtr right) => left.Equals(right);
    public static bool operator !=(MemoryBlockPtr left, MemoryBlockPtr right) => !left.Equals(right);

    public bool Equals(MemoryBlockPtr other) => Ptr == other.Ptr;
    public override bool Equals(object? obj) => obj is MemoryBlockPtr other && Equals(other);
    public override int GetHashCode() => ((IntPtr)Ptr).GetHashCode();
}

public unsafe struct MemoryBlock
{
    public static readonly int BlockSize = Unsafe.SizeOf<MemoryBlock>();

    public MemoryBlock* Next;
    private int _length;
    private int _cursor;

    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)Unsafe.AsPointer(ref this) + BlockSize, 0, _length);
    }

    public NativeAllocator GetAllocator(int alignment = 0)
    {
        var self = (MemoryBlock*)Unsafe.AsPointer(ref this);
        return new NativeAllocator(Data, ref self->_cursor, alignment);
    }

    public readonly int Cursor => _cursor;
    public readonly int Length => _length;
    public readonly int Remaining => _length - _cursor;

    public void ResetCursor() => _cursor = 0;

    internal void SetCursor(int cursor) => _cursor = cursor;
    internal void SetLength(int length) => _length = length;

    internal void Init(int length)
    {
        Next = null;
        _length = length;
        _cursor = 0;
    }
}