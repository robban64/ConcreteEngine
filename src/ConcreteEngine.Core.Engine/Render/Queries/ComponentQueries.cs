using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed partial class RenderWorld
{
    public static partial class Queries
    {
        public ref struct ComponentEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _i = -1;
            private readonly int _length;
            private readonly RenderComponentStore<T1> _store;

            public ComponentEnumerator()
            {
                _store = Store<T1>();
                _length = _store.Count;
            }

            public QueryItem<T1> Current { get; private set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _length)
                {
                    var entity = _store.GetEntity(_i);
                    if (entity.IsValid)
                    {
                        Current = new QueryItem<T1>(entity, ref _store.GetByIndex(_i));
                        return true;
                    }
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ComponentEnumerator<T1> GetEnumerator() => this;
        }

        public ref struct FilteredComponentEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _i;
            private readonly int _length;
            private readonly BitSet _filter;
            private readonly RenderComponentStore<T1> _store;

            public QueryItem<T1> Current { get; private set; }

            public FilteredComponentEnumerator(BitSet filter)
            {
                _i = -1;
                _store = Store<T1>();
                _length = _store.Count;
                _filter = filter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _length)
                {
                    var entity = _store.GetEntity(_i);
                    if (_filter[entity.Id])
                    {
                        Current = new QueryItem<T1>(entity, ref _store.GetByIndex(_i));
                        return true;
                    }
                }

                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly FilteredComponentEnumerator<T1> GetEnumerator() => this;
        }

        public ref struct SparseComponentEnumerator<T1> where T1 : unmanaged, IRenderComponent<T1>
        {
            private int _i;
            private readonly ReadOnlySpan<RenderEntity> _entities;

            public QueryItem<T1> Current { get; private set; }

            public SparseComponentEnumerator(ReadOnlySpan<RenderEntity> entities)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(entities.Length, Store<T1>().Count);
                _i = -1;
                _entities = entities;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _entities.Length)
                {
                    var entity = _entities[_i];
                    if (entity.IsValid)
                    {
                        Current = new QueryItem<T1>(entity, ref Store<T1>().Get(entity));
                        return true;
                    }
                }

                return false;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly SparseComponentEnumerator<T1> GetEnumerator() => this;
        }
    }
}

/*
    public ref struct JoinQueryEnumerator(NativeView<RenderEntity> entities, ReadOnlySpan<RenderEntity> right)
    {
        private readonly NativeView<RenderEntity> _entities = entities;
        private readonly ReadOnlySpan<RenderEntity> _right = right;
        private readonly int _length = entities.Length;
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _length)
            {
                var index = SearchMethod.BinarySearch(_right, _entities[_i]);
                if (index >= 0) return true;
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entities[_i], ref RenderWorld.Store<T>().Get(_entities[_i]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly JoinQueryEnumerator GetEnumerator() => this;
    }
*/