using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(NativeArray<byte> buffer)
{
    public UnsafeSpanWriter Sw = new(buffer.Ptr, buffer.Capacity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(char value)
    {
        var written = UtfText.FormatChar(Sw.Buffer, value);
        Sw.Buffer[written] = 0;
        return Sw.Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(int value)
    {
        var written = UtfText.Format(value, Sw.Buffer, Sw.Capacity);
        Sw.Buffer[written] = 0;
        return Sw.Buffer;
    }

    public readonly byte* Write(ushort value) => Write((int)value);
    public readonly byte* Write(short value) => Write((int)value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            Sw.Buffer[0] = 0;
            return Sw.Buffer;
        }

        var dest = MemoryMarshal.CreateSpan(ref *Sw.Buffer, Sw.Capacity - 1);
        Utf8.FromUtf16(value, dest, out _, out var written);
        Sw.Buffer[written] = 0;
        return Sw.Buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dest = MemoryMarshal.CreateSpan(ref *Sw.Buffer, Sw.Capacity - 1);
        value.TryFormat(dest, out var written, format, null);
        Sw.Buffer[written] = 0;
        return Sw.Buffer;
    }
}