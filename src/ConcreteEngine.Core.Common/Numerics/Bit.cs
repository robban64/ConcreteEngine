using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Bit256
{
    public const int Capacity = 256;
    private const ulong HighBit = 1UL << 63;

    public static Bit256 Zero { get; } = default;

    public static Bit256 AllBitsSet { get; } = new(Vector256<ulong>.AllBitsSet);

    public Vector256<ulong> _bits;

    public Bit256(Vector256<ulong> bits) => _bits = bits;

    public readonly bool IsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits != Vector256<ulong>.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector256<ulong>(Bit256 b) => b._bits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Bit256(Vector256<ulong> b) => Unsafe.BitCast<Vector256<ulong>, Bit256>(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ref Bit64 AsReadRef64() => ref Unsafe.As<Vector256<ulong>, Bit64>(ref Unsafe.AsRef(in _bits));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 GetBit64(int lane) => Unsafe.Add(ref AsReadRef64(), lane);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasBit(int bit) => GetBit64(bit >> 6)[bit];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Enable(ref Bit256 it, int bit)
    {
        ref var lane = ref Unsafe.Add(ref Unsafe.As<Bit256, Bit64>(ref it), bit >> 6);
        lane.Enable(bit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Disable(ref Bit256 it, int bit)
    {
        ref var lane = ref Unsafe.Add(ref Unsafe.As<Bit256, Bit64>(ref it), bit >> 6);
        lane.Disable(bit);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit256 And(Bit256 b1) => Unsafe.BitCast<Vector256<ulong>, Bit256>(Vector256.BitwiseAnd(_bits, b1));
    
    // left & ~right
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit256 AndNot(Bit256 b1) => Unsafe.BitCast<Vector256<ulong>, Bit256>(Vector256.AndNot(_bits, b1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit256 Or(Bit256 b1) => Unsafe.BitCast<Vector256<ulong>, Bit256>(Vector256.BitwiseOr(_bits, b1));

}

[StructLayout(LayoutKind.Sequential)]
public record struct Bit64
{
    public static Bit64 Zero { get; } = default;

    public static Bit64 All { get; } = new(ulong.MaxValue);

    public ulong Bits;

    public Bit64(ulong bits) => Bits = bits;

    public readonly bool IsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Bits != 0;
    }

    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Bits == 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(Bit64 b) => b.Bits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Bit64(ulong b) => Unsafe.BitCast<ulong, Bit64>(b);

    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Get(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(index, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Get(int index) => (Bits & (1UL << (index & 63))) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value)
    {
        if (value) Bits |= 1UL << index;
        else Bits &= ~(1UL << index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enable(int index) => Bits |= 1UL << index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disable(int index) => Bits &= ~(1UL << index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearLowerBits() => Bits &= Bits - 1;

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bit64 And(Bit64 b1, Bit64 b2) => Unsafe.BitCast<ulong, Bit64>(b1.Bits & b2.Bits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 And(Bit64 other) => Unsafe.BitCast<ulong, Bit64>(Bits & other.Bits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 Or(Bit64 other) => Unsafe.BitCast<ulong, Bit64>(Bits | other.Bits);
    //


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitEnumerator GetEnumerator() => new(this);

    public ref struct BitEnumerator(Bit64 bits)
    {
        private Bit64 _bits = bits;
        public int Current { get; private set; } = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_bits.IsSet)
            {
                Current = BitOperations.TrailingZeroCount(_bits);
                _bits.ClearLowerBits();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitEnumerator GetEnumerator() => this;
    }
}