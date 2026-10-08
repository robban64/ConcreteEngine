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
        public ref struct FilterQuery<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private int _lane;
            private int _blockCursor;
            private int _wordIndex;
            private readonly int _count;

            private Bit64 _currentBits;
            private Bit256 _bit256;

            private readonly BitSet _filter1;
            private readonly BitSet _filter2;

            public FilterQuery(BitSet filter1, BitSet filter2)
            {
                _lane = -1;
                _blockCursor = 0;
                _wordIndex = 0;
                _count = EntityBlockCount;
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;
                _bit256 = filter1.GetBit256(0).And(filter2.GetBit256(0));
            }
            
            public readonly FilterQueryItem<T1,T2> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_wordIndex << 6, _wordIndex, _currentBits);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (true)
                {
                    while (++_lane < 4)
                    {
                        _currentBits = _bit256.GetBit64(_lane);
                        if (_currentBits.IsEmpty) continue;
                        _wordIndex = (_blockCursor << 2) + _lane;
                        return true;
                    }

                    if (++_blockCursor >= _count) return false;
                    
                    var b1 = _filter1.GetBit256(_blockCursor << 2);
                    var b2 = _filter2.GetBit256(_blockCursor << 2);
                    _bit256 = b1.And(b2);
                    _lane = -1;
                }

            }
          
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1, T2> GetEnumerator() => this;
        }

        public readonly ref struct FilterQueryItem<T1, T2>(int start, int blockIndex, Bit64 bits)
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>

        {
            public readonly int Start = start;
            public readonly int BlockIndex = blockIndex;
            public readonly Bit64 Bits  = bits;
        
            public FilterEnumerator<T1,T2> Enumerator => new(Bits, Start, Dense<T1>().AsSpan().Slice(Start), Dense<T2>().AsSpan().Slice(Start));
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