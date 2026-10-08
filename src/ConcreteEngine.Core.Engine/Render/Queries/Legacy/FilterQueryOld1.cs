/*
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Query;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private readonly int _count;
            private int _blockCursor;
            private int _lane;

            private readonly BitOp _op;

            private Bit64 _currentBits;
            private Bit256 _bit256;

            private readonly BitSet _filter1;
            private readonly BitSet _filter2;

            private readonly Span<T1> _data1;

            public FilterQuery(BitOp op, BitSet filter1, BitSet filter2)
            {
                _count = EntityChunkCount;
                _lane = -1;
                _blockCursor = 0;
                _filter1 = filter1;
                _filter2 = filter2;
                _op = op;
                _data1 = Dense<T1>().AsSpan();
                _bit256 = ApplyFilter(op, filter1.GetBit256(0), filter2.GetBit256(0));
            }

            public readonly FilterQueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new((_blockCursor << 2) + _lane, _currentBits, _data1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (true)
                {
                    if (_bit256.IsSet)
                    {
                        while (++_lane < 4)
                        {
                            _currentBits = _bit256.GetBit64(_lane);
                            //_wordIndex = (_blockCursor << 2) + _lane;
                            if (_currentBits.IsSet) return true;
                        }
                    }

                    if (++_blockCursor >= _count) return false;

                    var b1 = _filter1.GetBit256(_blockCursor << 2);
                    var b2 = _filter2.GetBit256(_blockCursor << 2);
                    _bit256 = ApplyFilter(_op,  b1,  b2);
                    _lane = -1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }

        [SkipLocalsInit]
        public readonly ref struct FilterQueryItem<T1>(int wordIndex, Bit64 bits, Span<T1> data1)
            where T1 : unmanaged, IRenderComponent<T1>

        {
            public readonly int WordIndex = wordIndex;
            public readonly Bit64 Bits = bits;
            private readonly Span<T1> _data1 = data1;

            //private readonly FilterEnumerator<T1> _enumerator = new(bits, wordIndex << 6, data1.Slice(wordIndex << 6));

            public int Start => WordIndex << 6;
            public FilterEnumerator<T1> GetEnumerator() => new(Bits, Start, _data1.Slice(Start));
        }

        [SkipLocalsInit]
        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _bit;
            private int _entity;
            private readonly int _start;

            private Bit64 _currentBits;
            private readonly Span<T1> _data1;

            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1)
            {
                _bit = 0;
                _start = start;
                _entity = 0;
                _currentBits = entityBits;
                _data1 = data1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentBits.IsEmpty) return false;
                _bit = BitOperations.TrailingZeroCount(_currentBits);
                _entity = _bit + _start;
                _currentBits.ClearLowerBits();
                return true;
            }

            public readonly QueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_entity, _bit, ref _data1[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1> GetEnumerator() => this;
        }
    }
}
*/