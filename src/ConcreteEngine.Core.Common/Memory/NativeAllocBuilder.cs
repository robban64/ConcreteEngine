using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public ref struct NativeAllocBuilder
{
    public readonly NativeView<byte> Data;
    public readonly int Alignment;
    public int Cursor = 0;

    public NativeAllocBuilder(NativeView<byte> data, int alignment = 0)
    {
        if(alignment > 0 && !IntMath.IsPowerOfTwo(alignment))
            Throwers.InvalidArgument(nameof(alignment));
        
        Data = data;
        Alignment = alignment;
    }

    public bool IsNull => Data.IsNull;
    public int Length => Data.Length;
    public int Remaining => Data.Length - Cursor;

    public NativeView<byte> AllocSlice(int length)
    {
        var view = AllocViewSlice(Data, Cursor, length, Alignment);
        Cursor += length;
        return view;
    }

    public NativeView<byte> AllocStringSlice(ReadOnlySpan<char> str, bool nullTerminated = true)
    {
        var length = Encoding.UTF8.GetByteCount(str);
        if (nullTerminated) length += 1;
        var data = AllocSlice(length);
        int written = Encoding.UTF8.GetBytes(str, data.AsSpan());
        if (nullTerminated) data[written] = 0;
        return data;
    }

    public NativeView<T> AllocSlice<T>(int amount = 1) where T : unmanaged
    {
        return AllocSlice(Unsafe.SizeOf<T>() * amount).Reinterpret<T>();
    }
    
    public static NativeView<byte> AllocViewSlice(NativeView<byte> view, int cursor, int length, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        if (alignment > 0) length = IntMath.AlignUp(length, alignment);

        if ((uint)cursor + (uint)length > (uint)view.Length)
            Throwers.BufferOverflow(nameof(Data), cursor + length, length);

        return view.Slice(cursor, length);
    }
}