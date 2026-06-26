using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Memory;

public readonly ref struct NativeAllocator(NativeView<byte> data, ref int cursor, int alignment = 0)
{
    public readonly NativeView<byte> Data = data;
    public readonly ref int Cursor = ref cursor;
    public readonly int Alignment = alignment;

    public bool IsNull => Data.IsNull;
    public int Length => Data.Length;
    public int Remaining => Data.Length - Cursor;

    public NativeView<byte> AllocSlice(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        if (Alignment > 0) length = IntMath.AlignUp(length, Alignment);

        if ((uint)Cursor + (uint)length > (uint)Length)
            throw new InsufficientMemoryException(length.ToString());

        var start = Cursor;
        Cursor += length;
        return Data.Slice(start, length);
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return AllocSlice(Unsafe.SizeOf<T>() * amount).Reinterpret<T>();
    }
}