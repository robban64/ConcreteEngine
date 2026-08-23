using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Data;

internal static unsafe class ScratchBuffer
{
    private static NativeArray<byte> _buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeSpanWriter Writer() => new(_buffer.Ptr, _buffer.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeView<byte> WriteString(ReadOnlySpan<char> str)
    {
        var writer = Writer();
        return writer.Write(str);
    }

    public static void Create()
    {
        if (!_buffer.IsNull) throw new InvalidOperationException("Buffer is already created");
        _buffer = NativeArray.Allocate<byte>(512, false);
    }

    public static void Dispose()
    {
        _buffer.Dispose();
    }
}