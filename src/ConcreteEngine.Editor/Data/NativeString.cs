using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Data;

internal readonly unsafe struct NativeString : IEquatable<NativeString>
{
    public const int HeaderSize = 2 * sizeof(int);
    public static NativeString Null => new(null);

    private readonly NativeStringHeader* _ptr;

    public NativeString(NativeStringHeader* ptr) => _ptr = ptr;

    public bool IsNull => _ptr == null;
    public int Length => _ptr->Length;
    public int Capacity => _ptr->Capacity;
    public int Remaining => Capacity - Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => str.TextStart;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => str.Text;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => Text.AsSpan();

    public byte* TextStart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte*)_ptr + HeaderSize;
    }

    public byte* TextEnd
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TextStart + Length;
    }

    public NativeView<byte> Text
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(TextStart, Length);
    }

    public NativeView<byte> Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(TextStart, Capacity);
    }

    public NativeSpanWriter OverWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Data[0] = 0;
            _ptr->Length = 0;
            return new NativeSpanWriter(Data, Capacity);
        }
    }

    private NativeSpanWriter DataWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)Length >= (uint)Capacity) Throwers.BufferOverflow(nameof(Capacity), Length, Capacity);
            return new NativeSpanWriter(Data, Capacity, Length);
        }
    }

    public void CalculateLength()
    {
        var index = Data.AsReadOnlySpan().IndexOf((byte)0);
        SetLength(index >= 0 ? index : 0);
    }

    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)Capacity, nameof(length));
        Data[length] = 0;
        _ptr->Length = length;
    }

    public void Set(ReadOnlySpan<byte> str) => _ptr->Length = OverWriter.Write(str.Truncate(Capacity)).Length;
    public void Set(ReadOnlySpan<char> str) => _ptr->Length = OverWriter.Write(str.Truncate(Capacity)).Length;

    public void Append(ReadOnlySpan<byte> str) => _ptr->Length = DataWriter.Append(str).End().Length;
    public void Append(ReadOnlySpan<char> str) => _ptr->Length = DataWriter.Append(str).End().Length;

    public void Append<T>(T value, ReadOnlySpan<char> format = default)
        where T : IUtf8SpanFormattable
    {
        _ptr->Length = DataWriter.Append(value, format).End().Length;
    }

    public void Reset() => _ptr->Length = 0;

    public void Clear()
    {
        Data.Clear();
        _ptr->Length = 0;
    }

    public static bool operator ==(NativeString left, NativeString right) => left.Equals(right);
    public static bool operator !=(NativeString left, NativeString right) => !left.Equals(right);

    public bool Equals(NativeString other) => _ptr == other._ptr;

    public override bool Equals(object? obj) => obj is NativeString other && Equals(other);

    public override int GetHashCode() => unchecked((int)(long)_ptr);

    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeStringHeader(int capacity, int length = 0)
    {
        public int Length = length;
        public readonly int Capacity = capacity;
    }

    internal static NativeString From(NativeView<byte> view)
    {
        if (view.IsNullOrEmpty) Throwers.InvalidArgument(nameof(view));
        var ptr = (NativeStringHeader*)view.Ptr;
        *ptr = new NativeStringHeader(view.Length, 0);
        return new NativeString(ptr);
    }
    
}