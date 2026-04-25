using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

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
    public NativeView<byte> DataPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Ptr->DataPtr;
        }
    }
    public void ResetCursor() => Ptr->ResetCursor();

    public NativeView<byte> AllocSlice(int length) => Ptr->AllocSlice(length);

    public NativeView<T> AllocSlice<T>(int amount = 1) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return AllocSlice(Unsafe.SizeOf<T>() * amount).Reinterpret<T>();
    }

    public static implicit operator MemoryBlockPtr(MemoryBlock* ptr) => new(ptr);
    public static implicit operator MemoryBlockPtr(IntPtr ptr) => new((MemoryBlock*)ptr);
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

    public NativeView<byte> DataPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)Unsafe.AsPointer(ref this) + BlockSize, 0, _length);
    }

    public readonly int Cursor => _cursor;
    public readonly int Length => _length;
    public readonly int Remaining => _length - _cursor;

    public void ResetCursor() => _cursor = 0;
    internal void SetLength(int length) => _length = length;

    internal void Init(int length)
    {
        Next = null;
        _length = length;
        _cursor = 0;
    }


    public NativeView<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        length = IntMath.AlignUp(length, 4);
        if ((uint)_cursor + (uint)length > (uint)_length)
            throw new InsufficientMemoryException(length.ToString());

        var start = _cursor;
        _cursor += length;
        return DataPtr.Slice(start, length);
    }
}