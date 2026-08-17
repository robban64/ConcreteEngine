using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Data;

internal unsafe ref struct NativeStringBuilder : IDisposable
{
    private NativeSpanWriter _sw;
    private readonly NativeString _str;

    public NativeStringBuilder(NativeString str)
    {
        if (str.IsNull) Throwers.NullPointer(nameof(str));
        _str = str;
        _sw = new NativeSpanWriter(str.Data, str.Capacity);
    }

    [UnscopedRef]
    public ref NativeSpanWriter Writer => ref _sw;

    public readonly void Dispose() => _str.ApplyWriter(_sw);
}

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        return Text.AsSpan();
    }

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

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLength(int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)length, (uint)Capacity, nameof(length));
        TextStart[length] = 0;
        _ptr->Length = length;
    }

    public void Set(ReadOnlySpan<byte> str) => ApplyWriter(GetWriter().Append(str));
    public void Set(ReadOnlySpan<char> str) => ApplyWriter(GetWriter().Append(str));

    public void CalculateLength()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        var index = Data.AsReadOnlySpan().IndexOf((byte)0);
        SetLength(index >= 0 ? index : 0);
    }

    public void Reset() => _ptr->Length = 0;

    public void Clear()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        Data.Clear();
        _ptr->Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeSpanWriter GetWriter()
    {
        if (IsNull) Throwers.NullPointer(nameof(_ptr));
        return new NativeSpanWriter(TextStart, Capacity);
    }

    public void ApplyWriter(NativeSpanWriter sw, bool nullTerminated = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sw.Cursor);
        ArgumentOutOfRangeException.ThrowIfNotEqual((nint)sw.Buffer, (nint)TextStart);
        SetLength(sw.Cursor);
        if (nullTerminated) TextStart[sw.Cursor] = 0;
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

    internal static NativeString From(NativeView<byte> view)
    {
        if (view.IsNullOrEmpty) Throwers.InvalidArgument(nameof(view));
        var ptr = (NativeStringHeader*)view.Ptr;
        *ptr = new NativeStringHeader(view.Length);
        return new NativeString(ptr);
    }
}