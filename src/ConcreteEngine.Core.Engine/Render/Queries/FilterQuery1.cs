using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public ref struct FilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private readonly int _chunkCount;
            private int _chunkCursor;
            private Bit256 _currentChunk;

            private readonly QueryFilterData _filter;

            public FilterQuery(QueryFilterData filter)
            {
                _chunkCount = EntityChunkCount;
                _chunkCursor = -1;
                _filter = filter;
            }

            public readonly FilterQueryItem<T1> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_chunkCursor << 2, _currentChunk);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_chunkCursor < _chunkCount)
                {
                    _currentChunk = _filter.Apply(_chunkCursor << 2);
                    if (_currentChunk.IsSet) return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilterQuery<T1> GetEnumerator() => this;
        }

        public readonly ref struct FilterQueryItem<T1>(int blockIdx, Bit256 chunk)
            where T1 : unmanaged, IRenderComponent<T1>

        {
            public readonly int BlockIdx = blockIdx;
            public readonly Span<T1> Data1 = Dense<T1>().AsSpan();
            public readonly Bit256 Chunk = chunk;

            public int Lanes => int.Min(EntityBlockCount - BlockIdx, 4);

            public FilterEnumerator<T1> EnumerateLane(int lane)
            {
                return new FilterEnumerator<T1>(Chunk.GetBit64(lane), (BlockIdx + lane) << 6, Data1);
            }

            public delegate void QueryAction<TContext>(ref TContext ctx, int lane, int start, Bit64 block, Span<T1> data1) 
                where TContext : struct, allows ref struct;

            public void ForEachLane<TContext>(ref TContext ctx, QueryAction<TContext> action)
                where TContext : struct, allows ref struct
            {
                var lanes = Lanes;
                for (int lane = 0; lane < lanes; lane++)
                {
                    var chunk = Chunk.GetBit64(lane);
                    if (chunk.IsSet)
                    {
                        var blockIndex = (BlockIdx + lane) << 6;
                        action(ref ctx, lane, blockIndex, chunk, Data1);
                    }
                }
            }
        }

        public ref struct FilterEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private readonly int _start;
            private int _bit;
            private int _entity;

            private Bit64 _currentBits;
            private readonly Span<T1> _data1;

            public FilterEnumerator(Bit64 entityBits, int start, Span<T1> data1)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(start);
                _bit = 0;
                _start = start;
                _entity = 0;
                _currentBits = entityBits;
                _data1 = data1.Slice(start);
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