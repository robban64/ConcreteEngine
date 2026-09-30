using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ConcreteEngine.Core.Common.Collections;

public struct BitBlock
{
    public ulong Block;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Get(int index)
    {
        return (Block & (1UL << (index & 63))) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, bool value)
    {
        if (value) Block |= 1UL << index;
        else Block &= ~(1UL << index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleOn(int index) => Block |= 1UL << index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleOff(int index) => Block &= ~(1UL << index);
}

public readonly struct BitSet
{
    private readonly int _count;
    private readonly ulong[] _bits;

    public BitSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _count = capacity;
        _bits = new ulong[(capacity + 63) / 64];
    }

    public int Count => _count;
    public int BlockCount => _bits.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitBlock GetBlock(int blockIndex) => Unsafe.BitCast<ulong, BitBlock>(_bits[blockIndex]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int blockIndex, ulong block) => _bits[blockIndex] = block;

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
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count);
            return GetUnchecked(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count);
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
        if (value)
        {
            Array.Fill(_bits, ulong.MaxValue);
            ClearUnusedBits();
        }
        else
        {
            Clear();
        }
    }

    public void Invert()
    {
        var bits = _bits.AsSpan();
        for (int i = 0; i < bits.Length; i++) bits[i] = ~bits[i];
        ClearUnusedBits();
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
        return _count - countTrue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Array.Clear(_bits, 0, _bits.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearUnusedBits()
    {
        int remaining = _count & 63;
        if (remaining > 0)
        {
            ulong mask = (1UL << remaining) - 1;
            _bits[^1] &= mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_bits);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong PopCount(ulong i)
    {
        i = i - ((i >> 1) & 0x5555555555555555UL);
        i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
        i = (i + (i >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        return ((i * 0x0101010101010101UL) >> 56);
    }

    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<ulong> _bits;
        private ulong _currentBlock;

        private int _index;
        private int _blockIndex;

        internal Enumerator(ReadOnlySpan<ulong> bits)
        {
            _bits = bits;
            _currentBlock = 0;

            _index = -1;
            _blockIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_index + 1 >= _bits.Length) return false;
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
}