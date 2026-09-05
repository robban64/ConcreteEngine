using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public ref struct NativeAllocBuilder
{
    public readonly NativeView<byte> Data;
    public readonly int AlignCursor;
    public int Cursor;

    public NativeAllocBuilder(NativeView<byte> data, int cursor, int alignCursor)
    {
        if (data.IsNullOrEmpty) Throwers.NullPointer(nameof(data));
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)cursor, (ulong)data.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(alignCursor);

        if (alignCursor > 0 && !IntMath.IsPowerOfTwo(alignCursor))
            Throwers.InvalidArgument(nameof(alignCursor), "Alignment of cursor must be a power of 2");

        Data = data;
        AlignCursor = alignCursor;
        Cursor = cursor;
    }

    public NativeAllocBuilder(NativeView<byte> data, int alignCursor = 0) : this(data, 0, alignCursor) { }

    public readonly int Length => Data.Length;
    public readonly int Remaining => Data.Length - Cursor;

    public NativeView<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);
        if (Data.IsNullOrEmpty) Throwers.NullPointer(nameof(Data));

        var end = Cursor + length;
        if (AlignCursor > 0) end = IntMath.AlignUp(end, AlignCursor);

        if ((uint)end > (uint)Data.Length)
            Throwers.RangeOutOfBounds(Cursor, length, Data.Length, nameof(length));

        var view = Data.Slice(Cursor, length);
        Cursor = end;
        return view;
    }

    public NativeView<T> AllocSlice<T>(int amount) where T : unmanaged
    {
        return AllocSlice(Unsafe.SizeOf<T>() * amount).Reinterpret<T>();
    }

    public unsafe T* AllocRaw<T>(int amount) where T : unmanaged
    {
        var slice = AllocSlice(Unsafe.SizeOf<T>() * amount);
        return (T*)slice.Ptr;
    }

}
/*
    public NativeView<byte> AllocStringSlice(ReadOnlySpan<char> str, bool nullTerminated = true)
   {
       var length = Encoding.UTF8.GetByteCount(str);
       if (nullTerminated) length += 1;
       var data = AllocSlice(length);
       int written = Encoding.UTF8.GetBytes(str, data.AsSpan());
       if (nullTerminated) data[written] = 0;
       return data;
   }
*/