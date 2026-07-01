using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal readonly unsafe struct NativeString
{
    public const int HeaderSize = 2 * sizeof(int);

    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeStringHeader(int capacity, int length = 0)
    {
        public readonly int Capacity = capacity;
        public int Length = length;
        // byte[capacity]
    }

    internal static NativeString From(NativeView<byte> view)
    {
        var ptr = (NativeStringHeader*)view.Ptr;
        *ptr = new NativeStringHeader(view.Length, 0);
        return new NativeString(ptr);
    }
    //
    
    private readonly NativeStringHeader* _ptr;

    public NativeString(NativeStringHeader* ptr) => _ptr = ptr;

    public bool IsNull => _ptr == null;
    public int Length => _ptr->Length;
    public int Capacity => _ptr->Capacity;
    public int Remaining => _ptr->Capacity - _ptr->Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => (byte*)str._ptr + HeaderSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => str.Text;
    
    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)_ptr + HeaderSize, Capacity);
    }

    public NativeView<byte> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((byte*)_ptr + HeaderSize, Length);
    }
    
    public byte* TextEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte*)_ptr + HeaderSize + Length;
    }
    
    //TODO
    public NativeSpanWriter NewWrite
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            SetLength(0);
            return new NativeSpanWriter(Data, Capacity);
        }
    }

    private NativeSpanWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((uint)Length >= (uint)Capacity) Throwers.BufferOverflow(nameof(Capacity), Length, Capacity);
            return new NativeSpanWriter(Data, Capacity, Length);
        }
    }

    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)Capacity, nameof(length));
        _ptr->Length = length;
        Data[length] = 0;
    }
    
    public void Set(ReadOnlySpan<byte> str) => _ptr->Length = NewWrite.Write(str.Truncate(Capacity)).Length;
    public void Set(ReadOnlySpan<char> str)  => _ptr->Length = NewWrite.Write(str.Truncate(Capacity)).Length;
    
    public void Append(ReadOnlySpan<byte> str) => _ptr->Length = Writer.Append(str).End().Length;
    public void Append(ReadOnlySpan<char> str) => _ptr->Length = Writer.Append(str).End().Length;
    public void Append<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        _ptr->Length = Writer.Append(value, format).End().Length;
    }


    public void Clear() => _ptr->Length = 0;
}
