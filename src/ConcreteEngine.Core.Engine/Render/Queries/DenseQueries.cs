using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Query;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _blockIndex;
            private readonly int _blockLength;

            private readonly BitSet64 _filter1;
            private readonly BitSet64 _filter2;

            private readonly Span<T1> _data;

            public FilterQuery(BitSet64 filter1, BitSet64 filter2)
            {
                _blockIndex = -1;
                _blockLength = filter1.BlockCount;
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;
                _data = Dense<T1>().AsSpan();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_blockIndex < _blockLength)
                {
                    var block = _filter1.GetBlock(_blockIndex).And(_filter2.GetBlock(_blockIndex));
                    if (block.IsSet)
                    {
                        var start = _blockIndex * 64;
                        var length = int.Min(start + 64, _data.Length) - start;
                        Current = new FilterEnumerator<T1>(block, start, _data.Slice(start, length));
                        return true;
                    }
                }

                return false;
            }

            public FilterEnumerator<T1> Current { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }

        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private Bit64 _bits;
            private readonly Span<T1> _data;

            private int _bit;
            private readonly int _start;

            public FilterEnumerator(Bit64 bits, int start, Span<T1> data)
            {
                _bit = 0;
                _bits = bits;
                _start = start;
                _data = data;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (!_bits.IsSet) return false;

                var bit = BitOperations.TrailingZeroCount(_bits);
                _bits.ClearLowerBits();

                if (bit < _data.Length)
                {
                    _bit = bit;
                    return true;
                }

                return false;
            }


            public readonly QueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_start + _bit, ref _data[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1> GetEnumerator() => this;
        }


        public ref struct FilterQuery<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private int _blockIndex;
            //private readonly int _blockCount;
            // private readonly int _remaining;

            private readonly BitSet64 _filter1;
            private readonly BitSet64 _filter2;

            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;
            public FilterEnumerator<T1, T2> Current { get; private set; }

            public FilterQuery(BitSet64 filter1, BitSet64 filter2)
            {
                _blockIndex = -1;

                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;

                _data1 = Dense<T1>().AsSpan();
                _data2 = Dense<T2>().AsSpan();
                //_blockCount = (_data1.Length + 255) >> 8;
                //_remaining = _data1.Length & 255;

                if (_data1.Length != _data2.Length) Throwers.InvalidOperation();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var remaining = _data1.Length & 255;
                var blockCount = (_data1.Length + 255) >> 8;
                
                while (++_blockIndex < blockCount)
                {
                    var bit256 = _filter1.GetBlock256(_blockIndex << 2) & _filter2.GetBlock256(_blockIndex << 2);
                    if (bit256 != default)
                    {
                        int start = _blockIndex << 8;
                        int length = _blockIndex + 1 == blockCount && remaining != 0 ? remaining : 256;
                        Current = new FilterEnumerator<T1, T2>(bit256, start, _data1.Slice(start, length), _data2.Slice(start, length));
                        return true;
                    }
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1, T2> GetEnumerator() => this;
        }
    }

    public ref struct FilterEnumerator<T1, T2>
        where T1 : unmanaged, IRenderComponent<T1>
        where T2 : unmanaged, IRenderComponent<T2>

    {
        private ulong _currentBits;

        private readonly int _start;
        private int _lane;

        private readonly Vector256<ulong> _bit256;

        private readonly Span<T1> _data1;
        private readonly Span<T2> _data2;

        public QueryItem<T1, T2> Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FilterEnumerator(Vector256<ulong> bit256, int start, Span<T1> data1, Span<T2> data2)
        {
            _data1 = data1;
            _data2 = data2;
            _bit256 = bit256;
            _start = start;
            _lane = -1;
            _currentBits = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_currentBits == 0)
            {
                if (++_lane == 4) return false;
                _currentBits = _bit256.GetElement(_lane);
            }

            int bit = BitOperations.TrailingZeroCount(_currentBits);
            int bitIdx = (_lane << 6) + bit;
            _currentBits &= _currentBits - 1;

            if ((uint)bitIdx < (uint)_data1.Length)
            {
                Current = new QueryItem<T1, T2>(_start + bitIdx, ref _data1[bitIdx], ref _data2[bitIdx]);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FilterEnumerator<T1, T2> GetEnumerator() => this;
    }
}