using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Text;

public unsafe struct String64Utf8
{
    public const int Length = 64;

    private fixed byte _value[Length];
    public ref byte GetRef(int i = 0) => ref _value[i];
    public Span<byte> AsSpan(int start = 0, int length = Length) => MemoryMarshal.CreateSpan(ref _value[start], length);
}

internal unsafe struct String32Utf8
{
    public const int Length = 32;

    private fixed byte _value[Length];
    public ref byte GetRef(int i = 0) => ref _value[i];
    public Span<byte> AsSpan(int start = 0, int length = Length) => MemoryMarshal.CreateSpan(ref _value[start], length);
}

internal unsafe struct String16Utf8
{
    public const int Length = 16;

    private fixed byte _value[Length];
    public ref byte GetRef(int i = 0) => ref _value[i];
    public Span<byte> AsSpan(int start = 0, int length = Length) => MemoryMarshal.CreateSpan(ref _value[start], length);
}