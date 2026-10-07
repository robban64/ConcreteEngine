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
            private int _blockCursor;
            private readonly int _count;

            private readonly BitSet _filter1;
            private readonly BitSet _filter2;

            public FilterQueryItem<T1> Current { get; private set; }

            public FilterQuery(BitSet filter1, BitSet filter2)
            {
                _blockCursor = -1;
                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;
                _count = (EntityCount + 255) >> 8;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_blockCursor < _count)
                {
                    var blockIndex = _blockCursor << 2;
                    
                    var b1 = _filter1.GetBit256(blockIndex);
                    var b2 = _filter2.GetBit256(blockIndex);
                    var bit256 = Bit256.And(in b1, in b2);
                    if (bit256.IsSet)
                    {
                        int start = _blockCursor << 8;
                        Current = new FilterQueryItem<T1>(blockIndex, start, bit256);
                        return true;
                    }
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }
    }

    public readonly ref struct FilterQueryItem<T1>(int blockIndex, int start, Bit256 bit256)
        where T1 : unmanaged, IRenderComponent<T1>

    {
        public readonly int BlockIndex = blockIndex;
        public FilterEnumerator<T1> Enumerator => new(bit256, start, ref Dense<T1>()[start]);
    }

    public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>

    {
        private Bit64 _currentBits;

        private readonly int _start;
        private int _lane;

        private readonly Bit256 _bit256;

        private readonly ref T1 _data1;

        public QueryItem<T1> Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FilterEnumerator(Bit256 bit256, int start, ref T1 data1)
        {
             _data1 = ref data1;
            _bit256 = bit256;
            _start = start;
            _lane = -1;
            _currentBits = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_currentBits == 0)
            {
                if (++_lane == 4) return false;
                _currentBits = _bit256.GetBit64(_lane);
            }

            int bit = BitOperations.TrailingZeroCount(_currentBits.Block);
            int bitIdx = (_lane << 6) + bit;
            _currentBits.ClearLowerBits();
            
            Current = new QueryItem<T1>(_start + bitIdx, bitIdx, ref Unsafe.Add(ref _data1, bitIdx));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FilterEnumerator<T1> GetEnumerator() => this;
    }

}