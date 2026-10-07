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
        public ref struct FilterQuery<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            private int _blockIndex;

            private readonly BitSet _filter1;
            private readonly BitSet _filter2;

            private readonly Span<T1> _data1;
            private readonly Span<T2> _data2;
            public FilterQueryItem<T1, T2> Current { get; private set; }

            public FilterQuery(BitSet filter1, BitSet filter2)
            {
                _blockIndex = -1;

                _filter1 = filter1;
                _filter2 = filter2.IsNull ? Meta.TrueSet : filter2;

                _data1 = Dense<T1>().AsSpan();
                _data2 = Dense<T2>().AsSpan();

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
                        Current = new FilterQueryItem<T1, T2>(_blockIndex, new FilterEnumerator<T1, T2>(bit256, start,
                            _data1.Slice(start, length),
                            _data2.Slice(start, length)));
                        return true;
                    }
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1, T2> GetEnumerator() => this;
        }

        public ref struct FilterQueryItem<T1, T2>(int blockIndex, FilterEnumerator<T1, T2> enumerator)
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>

        {
            public readonly int BlockIndex = blockIndex;
            public readonly FilterEnumerator<T1, T2> Enumerator = enumerator;
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
}