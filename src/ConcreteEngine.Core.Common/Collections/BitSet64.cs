using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Common.Collections;

public record struct Bit64(ulong Block)
{
    public ulong Block = Block;

    public readonly bool IsSet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Block != 0;
    }
    
    public readonly bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Block == 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(Bit64 b) => b.Block;

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

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 And(Bit64 other) => Unsafe.BitCast<ulong, Bit64>(Block & other);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 And(ulong other) => Unsafe.BitCast<ulong, Bit64>(Block & other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Bit64 Or(Bit64 other) => Unsafe.BitCast<ulong, Bit64>(Block | other);
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

public readonly record struct BitSet64
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCapacity256(int bits) => IntMath.AlignUp((bits + 63) / 64, 4);

    private readonly ulong[] _bits;
    
    public BitSet64(int bitCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitCount);
        _bits = new ulong[GetCapacity256(bitCount)];
    }

    public BitSet64(ulong[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfZero(array.Length);
        _bits = array;
    }

    public bool IsNull => _bits is null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetOrTrue(int index) => _bits is null || this[index];

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
    public ulong GetRawBlock(int blockIndex) => _bits[blockIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetBlock(int blockIndex) => Unsafe.BitCast<ulong, Bit64>(_bits[blockIndex]);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit64 GetBlockAtBit(int index) => Unsafe.BitCast<ulong, Bit64>(_bits[index >> 6]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int blockIndex, ulong block) => _bits[blockIndex] = block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlockAtBit(int index, ulong block) => _bits[index >> 6] = block;
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<ulong> GetBlock256(int blockIndex)
    {
        return Vector256.LoadUnsafe(ref _bits[blockIndex]);
        //return Unsafe.As<ulong, Vector256<ulong>>(ref _bits[blockIndex]);
    }

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

    public BitSet64 Resized(int newSize)
    {
        if (_bits is null) Throwers.NullReference(nameof(_bits));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newSize);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, _bits.Length);

        var newArray = new ulong[newSize];
        Array.Copy(_bits, newArray, int.Min(_bits.Length, newSize));
        return new BitSet64(newArray);
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