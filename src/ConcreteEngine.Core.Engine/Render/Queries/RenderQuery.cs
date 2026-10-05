using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public readonly ref struct QueryItem<T1>(RenderEntity entity, ref T1 component) where T1 : unmanaged
        {
            public readonly RenderEntity Entity = entity;
            public readonly ref T1 Component = ref component;
        }

        public readonly ref struct QueryItem<T1, T2>(RenderEntity entity, ref T1 component1, ref T2 component2)
            where T1 : unmanaged where T2 : unmanaged
        {
            public readonly RenderEntity Entity = entity;
            public readonly ref T1 Component1 = ref component1;
            public readonly ref T2 Component2 = ref component2;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder<T1> New<T1>() where T1 : unmanaged, IRenderComponent<T1> => new();
        
        public readonly ref struct QueryBuilder<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            public FilterQuery<T1> Filter(BitSet filter) => new(filter);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FilteredComponentEnumerator<T1> VisibilityQuery<T1>() where T1 : unmanaged, IRenderComponent<T1>
        {
            return new FilteredComponentEnumerator<T1>(MetaStore.VisibleSet);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SparseComponentEnumerator<T1> SparseQuery<T1>(ReadOnlySpan<RenderEntity> entities)
            where T1 : unmanaged, IRenderComponent<T1>
        {
            return new SparseComponentEnumerator<T1>(entities);
        }
    }
}