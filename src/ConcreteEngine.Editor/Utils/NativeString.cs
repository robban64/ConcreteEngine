using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Utils;

internal unsafe struct NativeString
{
    private readonly MemoryBlockPtr _memory;
    private readonly int _capacity;
    private int _length;

    public readonly int Length => _length;
    public readonly int Capacity => _capacity;

    public NativeString(MemoryBlockPtr memory, int capacity, int length = 0)
    {
        if(memory.IsNull) Throwers.NullPointer(nameof(memory));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)capacity);

        _memory = memory;
        _length = length;
        _capacity = IntMath.AlignUp(capacity, 4);
    }

    public NativeString(MemoryBlockPtr memory, ReadOnlySpan<char> str) : this(memory, str.Length) => Set(str);
    public NativeString(MemoryBlockPtr memory, ReadOnlySpan<byte> str) : this(memory, str.Length) => Set(str);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => str._memory.Data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => new(str._memory.Data, str._length);

    public readonly NativeSpanWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if((uint)_length >= (uint)_capacity) Throwers.BufferOverflow(nameof(_capacity), _length, _capacity);
            return new NativeSpanWriter(_memory.Data, _capacity, _length);
        }
    }

    public readonly void Set(string str) => Writer.Write(str);
    public readonly void Set(ReadOnlySpan<char> str) => Writer.Write(str);
    public readonly void Set(ReadOnlySpan<byte> str) => Writer.Write(str);

    public void Clear() => _length = 0;
}
