using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Common.Text;

public unsafe ref partial struct NativeSpanWriter
{
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly NativeView<byte> ReturnWritten(int written)
    {
        if ((uint)written >= (uint)Capacity) Throwers.BufferOverflow(nameof(NativeSpanWriter), written, Capacity);
        Buffer[written] = 0;
        return new NativeView<byte>(Buffer, written);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(char value) => ReturnWritten(UtfText.FormatChar(ref *Buffer, value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(int value) => ReturnWritten(UtfText.Format(value, ref *Buffer, Capacity));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(uint value) => ReturnWritten(UtfText.Format(value, ref *Buffer, Capacity));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(scoped ReadOnlySpan<byte> value)
    {
        if (Validate(value.Length))
            Unsafe.CopyBlockUnaligned(ref *Buffer, ref MemoryMarshal.GetReference(value), (uint)value.Length);

        return ReturnWritten(value.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write(scoped ReadOnlySpan<char> value)
    {
        var written = Validate(value.Length) ? Encoding.UTF8.GetBytes(value, AsSpan()) : 0;
        return ReturnWritten(written);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NativeView<byte> Write<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(RemainingSpan(), out var written, format, null))
            Throwers.BufferOverflow(nameof(NativeSpanWriter), _cursor + written, Capacity);

        return ReturnWritten(written);
    }

    //
}