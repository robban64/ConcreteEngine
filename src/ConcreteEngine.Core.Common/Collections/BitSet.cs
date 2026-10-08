using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Collections;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct BitSet
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCapacity256(int bits) => IntMath.AlignUp((bits + 63) >> 6, 4);

    private readonly ulong[] _bits;
    
    public BitSet(int bitCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitCount);
        int count = GetCapacity256(bitCount);
        _bits = new ulong[count];
    }

    public BitSet(ulong[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfZero(array.Length);
        _bits = array;
    }

    public bool IsNull => _bits is null;
    
    public int BlockCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits.Length;
    }

    public int BitCapacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits.Length * 64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<ulong> AsSpan() => _bits;

    public bool this[int bit]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ulong block = _bits[bit >> 6];
            return (block & (1UL << (bit & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ulong mask = 1UL << bit;
            if (value) _bits[bit >> 6] |= mask;
            else _bits[bit >> 6] &= ~mask;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetOrTrue(int bit) => _bits is null || this[bit];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetBit64(int index) => Unsafe.BitCast<ulong, Bit64>(_bits[index]);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetAtBit64(int bit) => Unsafe.BitCast<ulong, Bit64>(_bits[bit >> 6]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit64(int index, Bit64 block) => _bits[index] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtBit64(int bit, ulong block) => _bits[bit >> 6] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit256 GetBit256(int index) =>  Unsafe.As<ulong, Bit256>(ref _bits[index]);

       // Unsafe.BitCast<Vector256<ulong>, Bit256>(Vector256.LoadUnsafe(ref _bits[index]));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit256(int index, in Bit256 block) => Unsafe.As<ulong, Bit256>(ref _bits[index]) = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock256(int index, Vector256<ulong> block) => block.StoreUnsafe(ref _bits[index]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle(int bit)
    {
        var b = this[bit];
        this[bit] = !b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnableBit(int bit) => this[bit] = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableBit(int bit) => this[bit] = false;

    public void SetAll(bool value)
    {
        if (value) Array.Fill(_bits, ulong.MaxValue);
        else Clear();
    }

    public void Invert()
    {
        var bits = _bits;
        for (int i = 0; i < bits.Length; i++) bits[i] = ~bits[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountTrue()
    {
        int count = 0;
        foreach (var bit in _bits) count += BitOperations.PopCount(bit);
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountFalse()
    {
        var countTrue = CountTrue();
        return BitCapacity - countTrue;
    }

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Array.Clear(_bits, 0, _bits.Length);

    public BitSet Resized(int newBitCount)
    {
        if (_bits is null) Throwers.NullReference(nameof(_bits));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newBitCount);

        var blockCount = GetCapacity256(newBitCount);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(blockCount, _bits.Length);

        var newArray = new ulong[blockCount];
        Array.Copy(_bits, newArray, int.Min(_bits.Length, blockCount));
        return new BitSet(newArray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitBlockEnumerator EnumerateBlocks(int count) => new(_bits, count);

    public ref struct BitBlockEnumerator
    {
        private readonly ReadOnlySpan<ulong> _blocks;
        private readonly int _elementCount;
        private int _index;

        public (int Start, int End, ulong Block) Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBlockEnumerator(ReadOnlySpan<ulong> blocks, int elementCount)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, blocks.Length * 64);
            _blocks = blocks;
            _elementCount = elementCount;
            _index = -1;
            Current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int nextBlock = _index + 1;
            int start = nextBlock << 6; // nextBlock * 64

            if (start >= _elementCount) return false;

            _index = nextBlock;
            int end = int.Min(start + 64, _elementCount);

            Current = (start, end, _blocks[nextBlock]);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBlockEnumerator GetEnumerator() => this;
    }
}