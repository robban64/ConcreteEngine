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

            public readonly ChunkQueryItem<T1> Current
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
        
        public ref struct FilterQuery<T1, T2> 
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
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

            public readonly ChunkQueryItem<T1, T2> Current
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
            public readonly FilterQuery<T1, T2> GetEnumerator() => this;
        }

    }
}