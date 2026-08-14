using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public readonly unsafe struct MemoryBlock : IEquatable<MemoryBlock>
{
    public const int HeaderSize = 16;

    public readonly MemoryBlockHeader* Ptr;

    private MemoryBlock(MemoryBlockHeader* block) => Ptr = block;

    public MemoryBlock(NativeView<byte> memory)
    {
        ArgumentNullException.ThrowIfNull(memory.Ptr);
        var ptr = (MemoryBlockHeader*)memory.Ptr;
        *ptr = new MemoryBlockHeader(null, memory.Length);
        Ptr = ptr;
    }

    public bool IsNull => Ptr == null;
    public int Cursor => Ptr->Cursor;
    public int Length => Ptr->Length;
    public int Remaining => Ptr->Remaining;

    public bool HasNext => Ptr != null && Ptr->Next != null;

    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)Cursor);
        Ptr->Length = length;
    }

    public void SetCursor(int cursor)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)cursor, (uint)Length);
        Ptr->Cursor = cursor;
    }

    public MemoryBlock Next
    {
        get
        {
            if (Ptr == null) Throwers.NullPointer(nameof(Ptr));
            if (Ptr->Next == null) Throwers.NullPointer(nameof(Next));
            return new MemoryBlock(Ptr->Next);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNext(out MemoryBlock block)
    {
        if (HasNext)
        {
            block = new MemoryBlock(Ptr->Next);
            return true;
        }

        block = default;
        return false;
    }

    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(Ptr != null);
            return new NativeView<byte>((byte*)Ptr + HeaderSize, Length);
        }
    }

    public NativeView<byte> AllocSlice(int length, int cursorAlignment = 0)
    {
        var allocator = new NativeAllocBuilder(Data, Cursor, cursorAlignment);
        var slice = allocator.AllocSlice(length);
        SetCursor(allocator.Cursor);
        return slice;
    }

    public static implicit operator MemoryBlock(MemoryBlockHeader* ptr) => new(ptr);
    public static implicit operator MemoryBlock(nint ptr) => new((MemoryBlockHeader*)ptr);
    public static explicit operator nint(MemoryBlock ptr) => (nint)ptr.Ptr;

    public static bool operator ==(MemoryBlock left, MemoryBlock right) => left.Equals(right);
    public static bool operator !=(MemoryBlock left, MemoryBlock right) => !left.Equals(right);

    public bool Equals(MemoryBlock other) => Ptr == other.Ptr;
    public override bool Equals(object? obj) => obj is MemoryBlock other && Equals(other);
    public override int GetHashCode() => ((nint)Ptr).GetHashCode();


    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBlockHeader
    {
        public MemoryBlockHeader* Next;
        public int Length;
        public int Cursor;

        public readonly int Remaining => Length - Cursor;

        internal MemoryBlockHeader(MemoryBlockHeader* next, int length)
        {
            Next = next;
            Length = length;
            Cursor = 0;
        }
    }
}