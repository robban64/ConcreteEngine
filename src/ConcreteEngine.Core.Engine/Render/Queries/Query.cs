using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public readonly ref struct QueryItem<T1>(int entity, ref T1 component) where T1 : unmanaged
        {
            public readonly int Entity = entity;
            public readonly ref T1 Component = ref component;
        }

        public readonly ref struct QueryItem<T1, T2>(int entity, ref T1 component1, ref T2 component2)
            where T1 : unmanaged where T2 : unmanaged
        {
            public readonly int Entity = entity;
            public readonly ref T1 Component1 = ref component1;
            public readonly ref T2 Component2 = ref component2;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder<T1> New<T1>() where T1 : unmanaged, IRenderComponent<T1> => new();
        
        public readonly ref struct QueryBuilder<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            public FilterQuery<T1> Filter(BitSet filter) => new(filter);
            
            public SparseFilterQuery<T1> SparseFilter(BitSet filter) => new(filter);

            public SparseEntityFilterQuery<T1> SparseEntityFilter(ReadOnlySpan<RenderEntity> entities, BitSet filter = default) => new(entities, filter);

        }
    }
}