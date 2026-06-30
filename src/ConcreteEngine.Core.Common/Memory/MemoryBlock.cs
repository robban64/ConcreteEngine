using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Memory;

public readonly unsafe struct MemoryBlockPtr : IEquatable<MemoryBlockPtr>
{
    public const int BlockSize = 16;
    
    public readonly MemoryBlock* Ptr;

    private MemoryBlockPtr(MemoryBlock* block) => Ptr = block;

    public MemoryBlockPtr(NativeView<byte> memory)
    {
        Ptr = (MemoryBlock*)memory.Ptr;
        Ptr->Init(memory.Length);
    }

    public bool IsNull => Ptr == null;
    public int Cursor => Ptr->Cursor;
    public int Length => Ptr->Length;
    public int Remaining => Ptr->Remaining;

    public void SetLength(int length) => Ptr->Length = length;

    public MemoryBlockPtr Next => new(Ptr->Next);

    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)Ptr + BlockSize, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> Slice(RangeU16 range) => Data.Slice(range);
    
    public NativeAllocator GetAllocator(int alignment = 0) => new(Data, ref Ptr->Cursor, alignment);

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
    
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBlock
    {
        public MemoryBlock* Next;
        public int Length;
        public int Cursor;

        public readonly int Remaining => Length - Cursor;

        internal void Init(int length)
        {
            Next = null;
            Length = length;
            Cursor = 0;
        }
    }
}
