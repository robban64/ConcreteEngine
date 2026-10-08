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
    public static int GetCapacity256(int bits) => IntMath.AlignUp((bits + 63) / 64, 4);

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
    
    public int BlockCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits.Length;
    }

    public int BitCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bits.Length * 64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<ulong> AsSpan() => _bits;

    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ulong block = _bits[index >> 6];
            return (block & (1UL << (index & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ulong mask = 1UL << index;
            if (value) _bits[index >> 6] |= mask;
            else _bits[index >> 6] &= ~mask;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetOrTrue(int index) => _bits is null || this[index];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetBit64(int blockIndex) => Unsafe.BitCast<ulong, Bit64>(_bits[blockIndex]);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetAtBit64(int index) => Unsafe.BitCast<ulong, Bit64>(_bits[index >> 6]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit64(int blockIndex, Bit64 block) => _bits[blockIndex] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAtBit64(int index, ulong block) => _bits[index >> 6] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit256 GetBit256(int blockIndex) =>  Unsafe.As<ulong, Bit256>(ref _bits[blockIndex]);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit256 GetBit256Safe(int blockIndex) =>  Unsafe.As<ulong, Bit256>(ref _bits[blockIndex << 2]);

       // Unsafe.BitCast<Vector256<ulong>, Bit256>(Vector256.LoadUnsafe(ref _bits[blockIndex]));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit256(int blockIndex, in Bit256 block) => Unsafe.As<ulong, Bit256>(ref _bits[blockIndex]) = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock256(int blockIndex, Vector256<ulong> block) => block.StoreUnsafe(ref _bits[blockIndex]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle(int index)
    {
        var bit = this[index];
        this[index] = !bit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnableBit(int index) => this[index] = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableBit(int index) => this[index] = false;

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
        ulong count = 0;
        foreach (var bit in _bits) count += PopCount(bit);
        return (int)count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountFalse()
    {
        var countTrue = CountTrue();
        return BitCount - countTrue;
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong PopCount(ulong i)
    {
        i = i - ((i >> 1) & 0x5555555555555555UL);
        i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
        i = (i + (i >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        return ((i * 0x0101010101010101UL) >> 56);
    }
/*
    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<ulong> _bits;
        private ulong _currentBlock;

        private int _index;
        private int _blockIndex;

        internal Enumerator(ReadOnlySpan<ulong> bits, int elementCount)
        {
            _bits = bits;
            _elementCount = elementCount;
            _currentBlock = 0;

            _index = -1;
            _blockIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_index + 1 >= _elementCount) return false;
            if ((++_index & 63) == 0) _currentBlock = _bits[_blockIndex++];
            return true;
        }

        public bool Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_currentBlock & (1UL << (_index & 63))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => this;
    }
    */

    public ref struct BitBlockEnumerator
    {
        private readonly ReadOnlySpan<ulong> _blocks;
        private readonly int _elementCount;
        private int _blockIndex;

        public (int Start, int End, ulong Block) Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBlockEnumerator(ReadOnlySpan<ulong> blocks, int elementCount)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(elementCount, blocks.Length * 64);
            _blocks = blocks;
            _elementCount = elementCount;
            _blockIndex = -1;
            Current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int nextBlock = _blockIndex + 1;
            int start = nextBlock << 6; // nextBlock * 64

            if (start >= _elementCount) return false;

            _blockIndex = nextBlock;
            int end = int.Min(start + 64, _elementCount);

            Current = (start, end, _blocks[nextBlock]);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitBlockEnumerator GetEnumerator() => this;
    }
}