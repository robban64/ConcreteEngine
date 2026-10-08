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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Bit256 CombineBlocks(Bit256 left, Bit256 right, BitOp op)
        {
            return  op switch
            {
                BitOp.And    => left.And(right),
                BitOp.AndNot => left.AndNot(right), 
                BitOp.Or     => left.Or(right),
                _            => Bit256.Zero
            };
        }
        [SkipLocalsInit]
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private readonly int _count;
            private int _blockCursor;
            private int _wordIndex;
            private int _lane;

            private BitOp _op;

            private Bit64 _currentBits;
            private Bit256 _bit256;

            private readonly BitSet _filter1;
            private readonly BitSet _filter2;

            private readonly Span<T1> _data1;

            public FilterQuery(BitOp op, BitSet filter1, BitSet filter2)
            {
                _count = EntityBlockCount;
                _lane = -1;
                _blockCursor = 0;
                _wordIndex = 0;
                _filter1 = filter1;
                _filter2 = filter2;
                _op = op;
                _data1 = Dense<T1>().AsSpan();
                _bit256 = CombineBlocks(filter1.GetBit256(0), filter2.GetBit256(0), _op);
            }

            public readonly FilterQueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_wordIndex, _wordIndex << 6, _currentBits, _data1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (true)
                {
                    while (++_lane < 4)
                    {
                        _currentBits = _bit256.GetBit64(_lane);
                        _wordIndex = (_blockCursor << 2) + _lane;
                        if (_currentBits.IsSet) return true;
                    }

                    if (++_blockCursor >= _count) return false;

                    var b1 = _filter1.GetBit256(_blockCursor << 2);
                    var b2 = _filter2.GetBit256(_blockCursor << 2);
                    _bit256 = CombineBlocks(b1, b2, _op);
                    _lane = -1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }

        [SkipLocalsInit]
        public readonly ref struct FilterQueryItem<T1>(int wordIndex, int entityIndex, Bit64 bits, Span<T1> data1)
            where T1 : unmanaged, IRenderComponent<T1>

        {
            public readonly int WordIndex = wordIndex;
            //public readonly Bit64 Bits = bits;
            // private readonly Span<T1> _data1 = data1;

            private readonly FilterEnumerator<T1> _enumerator = new(bits, entityIndex, data1.Slice(entityIndex));

            public int Start => WordIndex << 6;
            public FilterEnumerator<T1> GetEnumerator() => _enumerator; //new(Bits, Start, _data1.Slice(Start));
        }

        [SkipLocalsInit]
        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _bit;
            private readonly int _start;

            private Bit64 _currentBits;
            private readonly Span<T1> _data1;

            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1)
            {
                _bit = 0;
                _start = start;
                _currentBits = entityBits;
                _data1 = data1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentBits.IsEmpty) return false;
                _bit = BitOperations.TrailingZeroCount(_currentBits);
                _currentBits.ClearLowerBits();
                return true;
            }

            public readonly QueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_bit + _start, _bit, ref _data1[_bit]);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1> GetEnumerator() => this;
        }
    }
}