using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
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
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SparseFilterQuery<T1> SparseFilter<T1>(BitSet filter)
            where T1 : unmanaged, IRenderComponent<T1> => new(filter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SparseEntityFilterQuery<T1> SparseEntityFilter<T1>(ReadOnlySpan<RenderEntity> entities,
            BitSet filter = default)
            where T1 : unmanaged, IRenderComponent<T1> => new(entities, filter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FilterQuery<T1> EntityFilter<T1>() where T1 : unmanaged, IRenderComponent<T1>
        {
            return new FilterQuery<T1>(new QueryFilterData2(BitOp.And, Meta.EntitySet, default));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder<T1, T2> New<T1, T2>()
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2> => new();


        public readonly ref struct QueryBuilder<T1, T2>
            where T1 : unmanaged, IRenderComponent<T1>
            where T2 : unmanaged, IRenderComponent<T2>
        {
            public FilterQuery<T1, T2> Filter(BitOp op, BitSet filter1, BitSet filter2 = default, BitSet filter3 = default)
            {
                var filter = new QueryFilterData(op, filter1.IsNull ? Meta.EntitySet : filter1,
                    filter2.IsNull ? Meta.TrueSet : filter2, filter3);
                
                return new FilterQuery<T1, T2>(filter);
            }

        }
    }
}