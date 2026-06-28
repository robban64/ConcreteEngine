using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

public unsafe struct NativeString
{
    private readonly byte* _data;
    private readonly int _capacity;
    private int _length;

    public NativeString(byte* data, int capacity, int length = 0)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4);

        _data = data;
        _length = length;
        _capacity = capacity;
    }

    public NativeString(NativeView<byte> view) : this(view.Ptr, view.Length){}
    public NativeString(NativeView<byte> view, ReadOnlySpan<char> str) : this(view) => Set(str);
    public NativeString(NativeView<byte> view, ReadOnlySpan<byte> str) : this(view) => Set(str);


    public readonly int Length => _length;
    public readonly int Capacity => _capacity;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => str._data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => new(str._data, str._capacity);

    public readonly NativeSpanWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((uint)_length >= (uint)_capacity) Throwers.BufferOverflow(nameof(_capacity), _length, _capacity);
            return new NativeSpanWriter(_data, _capacity, _length);
        }
    }

    public void Set(ReadOnlySpan<char> str) => Writer.Write(str);
    public void Set(ReadOnlySpan<byte> str) => Writer.Write(str);

    public void Clear() => _length = 0;
}