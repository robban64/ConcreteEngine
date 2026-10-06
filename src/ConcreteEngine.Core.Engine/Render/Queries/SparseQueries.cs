using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Query
    {
        public readonly ref struct SparseQueryItem<T1>(RenderEntity entity, ref T1 component) where T1 : unmanaged
        {
            public readonly RenderEntity Entity = entity;
            public readonly ref T1 Component = ref component;
        }

        public readonly ref struct SparseQueryItem<T1, T2>(RenderEntity entity, ref T1 component1, ref T2 component2)
            where T1 : unmanaged where T2 : unmanaged
        {
            public readonly RenderEntity Entity = entity;
            public readonly ref T1 Component1 = ref component1;
            public readonly ref T2 Component2 = ref component2;
        }

        public ref struct SparseFilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _i;
            private readonly int _length;
            private readonly BitSet64 _filter;

            public SparseQueryItem<T1> Current { get; private set; }

            public SparseFilterQuery(BitSet64 filter)
            {
                _i = -1;
                _length = Sparse<T1>().Count;
                _filter = filter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _length)
                {
                    var entity = Sparse<T1>().GetEntity(_i);
                    if (_filter[entity.Id])
                    {
                        Current = new SparseQueryItem<T1>(entity, ref Sparse<T1>().GetByIndex(_i));
                        return true;
                    }
                }

                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SparseFilterQuery<T1> GetEnumerator() => this;
        }

        public ref struct SparseEntityFilterQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _i;
            private readonly BitSet64 _filter;
            private readonly ReadOnlySpan<RenderEntity> _entities;
            
            public SparseQueryItem<T1> Current { get; private set; }

            public SparseEntityFilterQuery(ReadOnlySpan<RenderEntity> entities, BitSet64 filter = default)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(entities.Length, Sparse<T1>().Count);
                _i = -1;
                _entities = entities;
                _filter = filter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _entities.Length && (_filter.IsNull || _filter[_i]))
                {
                    var entity = _entities[_i];
                    if (entity.IsValid)
                    {
                        Current = new SparseQueryItem<T1>(entity, ref Sparse<T1>().Get(entity));
                        return true;
                    }
                }

                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SparseEntityFilterQuery<T1> GetEnumerator() => this;
        }
    }
}
