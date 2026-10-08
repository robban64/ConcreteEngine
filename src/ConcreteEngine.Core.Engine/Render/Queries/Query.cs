using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;


public sealed partial class RenderWorld
{
    private static int EntityChunkCount => (EntityCount + 255) >> 8;
    private static int EntityBlockCount =>  (EntityCount + 63) >> 6;


    public static partial class Query
    {
        public readonly ref struct QueryItem<T1>(int entity, int entityBit, ref T1 component) where T1 : unmanaged
        {
            public readonly int Entity = entity;
            public readonly int EntityBit = entityBit;
            public readonly ref T1 Component = ref component;
        }

        public readonly ref struct QueryItem<T1, T2>(int entity, int entityBit, ref T1 component1, ref T2 component2)
            where T1 : unmanaged where T2 : unmanaged
        {
            public readonly int Entity = entity;
            public readonly int EntityBit = entityBit;
            public readonly ref T1 Component1 = ref component1;
            public readonly ref T2 Component2 = ref component2;
        }
        
        
        [SkipLocalsInit]
        public readonly ref struct ActionQueryItem<T1, T2>(int lane, int chunkStart, Span<T1> data1, Span<T1> data2)
            where T1 : unmanaged, IRenderComponent<T1>

        {
            public readonly int Lane = lane;
            public readonly int ChunkStart = chunkStart;
            public readonly Span<T1> Data1 = data1;
            public readonly Span<T1> Data2 = data2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder<T1> New<T1>() where T1 : unmanaged, IRenderComponent<T1> => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder<T1, T2> New<T1, T2>()
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2> => new();

        public readonly ref struct QueryBuilder<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            public FilterQuery<T1> Filter(BitOp op = BitOp.And, BitSet filter1 = default, BitSet filter2 = default)
            {
                var filter = new QueryFilterData(op, filter1.IsNull ? Meta.EntitySet : filter1,
                    filter2.IsNull ? Meta.TrueSet : filter2);
                return new FilterQuery<T1>(filter);
            }

            public SparseFilterQuery<T1> SparseFilter(BitSet filter) => new(filter);

            public SparseEntityFilterQuery<T1> SparseEntityFilter(ReadOnlySpan<RenderEntity> entities,
                BitSet filter = default) => new(entities, filter);
        }

        public readonly ref struct QueryBuilder<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            public FilterQuery<T1, T2> Filter(BitOp op, BitSet filter1, BitSet filter2 = default)
            {
                var filter = new QueryFilterData(op, filter1.IsNull ? Meta.EntitySet : filter1,
                    filter2.IsNull ? Meta.TrueSet : filter2);
                
                return new FilterQuery<T1, T2>(filter);
            }
        }
    }
}