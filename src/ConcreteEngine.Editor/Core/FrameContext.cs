using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(NativeArray<byte> buffer)
{
    private readonly byte* _buffer = buffer;
    private readonly int _capacity = buffer.Capacity;

    public ref UnsafeSpanWriter Sw
    {
        [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.As<FrameContext, UnsafeSpanWriter>(ref this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Write(char value)
    {
        var written = UtfText.FormatChar(_buffer, value);
        _buffer[written] = 0;
        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Write(int value)
    {
        var written = UtfText.Format(value, _buffer, _capacity);
        _buffer[written] = 0;
        return _buffer;
    }

    public byte* Write(ushort value) => Write((int)value);
    public byte* Write(short value) => Write((int)value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            _buffer[0] = 0;
            return _buffer;
        }
        
        var dest = MemoryMarshal.CreateSpan(ref *_buffer, _capacity - 1);
        Utf8.FromUtf16(value, dest, out _, out var written);
        _buffer[written] = 0;
        return _buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* Write<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        var dest = MemoryMarshal.CreateSpan(ref *_buffer, _capacity - 1);
        value.TryFormat(dest, out var written, format, null);
        _buffer[written] = 0;
        return _buffer;
    }
}