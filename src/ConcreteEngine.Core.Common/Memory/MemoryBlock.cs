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

    public void SetLength(int length) => Ptr->Length = length;
    public void SetCursor(int cursor) => Ptr->Cursor = cursor;

    public MemoryBlock Next
    {
        get
        {
            if(Ptr == null) Throwers.NullPointer(nameof(Ptr));
            if(Ptr->Next == null) Throwers.NullPointer(nameof(Next));
            return new MemoryBlock(Ptr->Next);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetNext(out MemoryBlock block)
    {
        block = Next;
        return block != null;
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

    public NativeView<byte> AllocSlice(int length, int alignment = 0)
    {
        if(Ptr == null) Throwers.NullPointer(nameof(Next));

        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);
        if (alignment > 0) length = IntMath.AlignUp(length, alignment);

        if ((uint)Cursor + (uint)length > (uint)Length)
            Throwers.BufferOverflow(nameof(Data), Cursor + length, Length);

        var start = Cursor;
        Ptr->Cursor += length;
        return Data.Slice(start, length);
    }

    public static implicit operator MemoryBlock(MemoryBlockHeader* ptr) => new(ptr);
    public static implicit operator MemoryBlock(IntPtr ptr) => new((MemoryBlockHeader*)ptr);
    public static explicit operator IntPtr(MemoryBlock ptr) => (IntPtr)ptr.Ptr;

    public static bool operator ==(MemoryBlock left, MemoryBlock right) => left.Equals(right);
    public static bool operator !=(MemoryBlock left, MemoryBlock right) => !left.Equals(right);

    public bool Equals(MemoryBlock other) => Ptr == other.Ptr;
    public override bool Equals(object? obj) => obj is MemoryBlock other && Equals(other);
    public override int GetHashCode() => ((IntPtr)Ptr).GetHashCode();


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