using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Collections;

public record struct BitBlock(ulong Block)
{
    public ulong Block = Block;

    public readonly bool IsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Block != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(BitBlock b) => b.Block;

    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Get(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(index, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Get(int index) => (Block & (1UL << (index & 63))) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value)
    {
        if (value) Block |= 1UL << index;
        else Block &= ~(1UL << index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enable(int index) => Block |= 1UL << index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Disable(int index) => Block &= ~(1UL << index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearLowerBits() => Block &= Block - 1;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BitEnumerator GetEnumerator() => new (this);

    public ref struct BitEnumerator(BitBlock bits)
    {
        private BitBlock _bits = bits;
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

public readonly struct BitSet
{
    private readonly ulong[] _bits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBlockCapacity(int bits) => (bits + 63) / 64;

    public BitSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _bits = new ulong[GetBlockCapacity(capacity)];
    }

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

    public Span<ulong> AsSpan() => _bits;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetRawBlock(int blockIndex) => _bits[blockIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitBlock GetBlockAtBit(int index) => Unsafe.BitCast<ulong, BitBlock>(_bits[index >> 6]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int blockIndex, ulong block) => _bits[blockIndex] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlockAtBit(int index, ulong block) => _bits[index >> 6] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetUnchecked(int index)
    {
        ulong block = _bits[index >> 6];
        return (block & (1UL << (index & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnchecked(int index, bool value)
    {
        ulong mask = 1UL << index;
        if (value) _bits[index >> 6] |= mask;
        else _bits[index >> 6] &= ~mask;
    }

    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)BitCount);
            return GetUnchecked(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)BitCount);
            SetUnchecked(index, value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Toggle(int index)
    {
        var bit = GetUnchecked(index);
        SetUnchecked(index, !bit);
    }

    public void SetAll(bool value)
    {
        if (value) Array.Fill(_bits, ulong.MaxValue);
        else Clear();
    }

    public void Invert()
    {
        var bits = _bits.AsSpan();
        for (int i = 0; i < bits.Length; i++) bits[i] = ~bits[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountTrue()
    {
        ulong count = 0;
        foreach (var bit in _bits.AsSpan()) count += PopCount(bit);
        return (int)count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountFalse()
    {
        var countTrue = CountTrue();
        return BitCount - countTrue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Array.Clear(_bits, 0, _bits.Length);


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