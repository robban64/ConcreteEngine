using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Data;

internal readonly unsafe struct NativeString : IEquatable<NativeString>
{
    public const int HeaderStride = 2 * sizeof(int);

    public static NativeString Null => new(null);

    private readonly NativeStringHeader* _ptr;

    private NativeString(NativeStringHeader* ptr) => _ptr = ptr;

    public bool IsNull => _ptr == null;
    public int Length => _ptr->Length;
    public int Capacity => _ptr->Capacity;
    public int Remaining => Capacity - Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte*(NativeString str) => str.TextStart;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeView<byte>(NativeString str) => str.Text;

    public byte* TextStart
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => &_ptr->Start;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsTextSpan()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        return new Span<byte>(TextStart, Length);
    }

    //
    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)Capacity, nameof(length));
        if (Remaining > 0) TextStart[length] = 0;
        _ptr->Length = length;
    }

    public void CalculateLength()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        var index = Data.AsReadOnlySpan().IndexOf((byte)0);
        SetLength(index >= 0 ? index : 0);
    }

    public void Reset() => _ptr->Length = 0;

    public void ClearText()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        Data.Clear();
        _ptr->Length = 0;
    }

    public NativeSpanWriter GetWriter()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        return new NativeSpanWriter(TextStart, Capacity + 1);
    }

    public void Set(ReadOnlySpan<byte> value)
    {
        Ensure(value.Length);
        value.CopyTo(Data.AsSpan());
        SetLength(value.Length);
    }

    public void Set(ReadOnlySpan<char> value)
    {
        Ensure(Encoding.UTF8.GetByteCount(value));
        var written = Encoding.UTF8.GetBytes(value, Data.AsSpan());
        SetLength(written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Ensure(int length)
    {
        if ((uint)length > (uint)Capacity)
            Throwers.BufferOverflow(nameof(NativeSpanWriter), length + Length, Capacity);
    }


    public static bool operator ==(NativeString left, NativeString right) => left.Equals(right);
    public static bool operator !=(NativeString left, NativeString right) => !left.Equals(right);

    public bool Equals(NativeString other) => _ptr == other._ptr;

    public override bool Equals(object? obj) => obj is NativeString other && Equals(other);

    public override int GetHashCode() => unchecked((int)(long)_ptr);

    //
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeStringHeader(int capacity, int length = 0)
    {
        public int Length = length;

        public readonly int Capacity = capacity;

        //
        public byte Start;
    }

    internal static NativeString Create(NativeView<byte> view)
    {
        if (view.IsNull) Throwers.NullPointer(nameof(view));

        var textCapacity = view.Length - HeaderStride;
        ArgumentOutOfRangeException.ThrowIfLessThan(textCapacity, 4, nameof(view));

        var ptr = (NativeStringHeader*)view.Ptr;
        *ptr = new NativeStringHeader(textCapacity);
        return new NativeString(ptr);
    }

    internal static NativeString From(byte* text, int capacity)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        var header = (NativeStringHeader*)(text - HeaderStride);
        if (header->Capacity != capacity - 1)
            Throwers.InvalidOperation("Invalid pointer - Capacity mismatch");

        return new NativeString(header);
    }
}