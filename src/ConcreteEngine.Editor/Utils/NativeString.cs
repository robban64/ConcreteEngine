using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal unsafe struct NativeString
{
    public const int HeaderSize = 2 * sizeof(int);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeStringData(int capacity, int length = 0)
    {
        public readonly int Capacity = capacity;
        public int Length = length;
        // byte[capacity]
    }

    internal static NativeString From(NativeView<byte> view)
    {
        var ptr = (NativeStringData*)view.Ptr;
        *ptr = new NativeStringData(view.Length, 0);
        return new NativeString(ptr);
    }
    
    //
    
    public NativeStringData* Ptr;

    public NativeString(NativeStringData* ptr) => Ptr = ptr;

    public readonly bool IsNull => Ptr == null;
    public readonly int Length => Ptr->Length;
    public readonly int Capacity => Ptr->Capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => (byte*)str.Ptr + HeaderSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => str.TextView;

    public readonly byte* EndPtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte*)Ptr + HeaderSize + Length;
    }
    
    public readonly NativeView<byte> TextView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)Ptr + HeaderSize, Length);
    }
    
    public readonly NativeView<byte> DataView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)Ptr + HeaderSize, Capacity);
    }
    
    //TODO Rename
    public readonly NativeSpanWriter NewWrite
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SetLength(0);
            return new NativeSpanWriter(DataView, Capacity);
        }
    }

    private readonly NativeSpanWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((uint)Length >= (uint)Capacity) Throwers.BufferOverflow(nameof(Capacity), Length, Capacity);
            return new NativeSpanWriter(DataView, Capacity, Length);
        }
    }

    public readonly void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)Capacity, nameof(length));
        Ptr->Length = length;
        DataView[length] = 0;
    }

    public readonly void Set(ReadOnlySpan<char> str)  => Ptr->Length = Writer.Write(str.Truncate(Capacity)).Length;
    public readonly void Set(ReadOnlySpan<byte> str) => Ptr->Length = Writer.Write(str.Truncate(Capacity)).Length;

    public readonly void Clear() => Ptr->Length = 0;
}
