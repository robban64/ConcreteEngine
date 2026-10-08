using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;
using static ConcreteEngine.Core.Engine.Render.RenderWorld.Query;

namespace ConcreteEngine.Core.Engine.Render;
/*
public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterQuery<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private readonly int _count;
            private int _blockCursor;
            private int _lane;

            private Bit64 _currentBits;
            private Bit256 _bit256;

            private readonly QueryFilterData _filter;

            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;

            public FilterQuery(BitOp op, BitSet filter1, BitSet filter2)
            {
                _count = EntityChunkCount;
                _lane = -1;
                _blockCursor = 0;
                //_wordIndex = 0;
                _filter = new QueryFilterData(op, filter1, filter2);
                _data1 = Dense<T1>().AsSpan();
                _data2 = Dense<T2>().AsSpan();
                _bit256 = ApplyFilter(op, filter1.GetBit256(0), filter2.GetBit256(0));
            }

            public readonly FilterQueryItem<T1,T2> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new((_blockCursor << 2) + _lane, _currentBits, _data1, _data2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (true)
                {
                    while (++_lane < 4)
                    {
                        _currentBits = _bit256.GetBit64(_lane);
                        if (_currentBits.IsSet) return true;
                    }
                    
                    if (++_blockCursor >= _count) return false;

                    _bit256 = _filter.Apply(_blockCursor << 2);
                    _lane = -1;
                }
            }

          
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1, T2> GetEnumerator() => this;
        }

        public readonly ref struct FilterQueryItem<T1, T2>(int wordIndex, Bit64 bits, Span<T1> data1, Span<T2> data2)
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>

        {
            public readonly int WordIndex = wordIndex;
            public readonly Bit64 Bits = bits;
            private readonly Span<T1> _data1 = data1;
            private readonly Span<T2> _data2 = data2;

            public int Start => WordIndex << 6;
            public FilterEnumerator<T1,T2> GetEnumerator() => new(Bits, Start, _data1.Slice(Start), _data2.Slice(Start));
        }

        public ref struct FilterEnumerator<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>

        {
            private int _bit;
            private readonly int _start;
        
            private Bit64 _currentBits;

            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1, Span<T2> data2)
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(data1.Length, data2.Length);
                _bit = 0;
                _start = start;
                _currentBits = entityBits;
                _data1 = data1;
                _data2 = data2;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentBits.IsEmpty) return false;
                _bit = BitOperations.TrailingZeroCount(_currentBits);
                _currentBits.ClearLowerBits();
                return true;
            }

            public readonly QueryItem<T1, T2> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_bit + _start, _bit, ref _data1[_bit], ref _data2[_bit]);
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterEnumerator<T1, T2> GetEnumerator() => this;
        }
    }
}
*/