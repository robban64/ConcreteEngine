using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS.Render.Components;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed partial class RenderComponentStore<T> where T : unmanaged, IRenderComponent<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentEnumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitSetQueryEnumerator VisibilityQuery() => new(this, RenderWorld.Core.Data.VisibleSet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator SparseQuery(ReadOnlySpan<RenderEntity> entities) => new(this, entities);

    public readonly ref struct RenderQueryItem(RenderEntity entity, ref T component)
    {
        public readonly RenderEntity Entity = entity;
        public readonly ref T Component = ref component;
    }

    public ref struct ComponentEnumerator(RenderComponentStore<T> store)
    {
        private int _i = -1;
        private readonly int _count = store.Count;

        public RenderQueryItem Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.IsValid)
                {
                    Current = new RenderQueryItem(entity, ref store.GetByIndex(_i));
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ComponentEnumerator GetEnumerator() => this;
    }

    public ref struct BitSetQueryEnumerator
    {
        private int _i;
        private readonly int _length;
        private readonly BitSet _bits;
        private readonly RenderComponentStore<T> _store;

        public RenderQueryItem Current { get; private set; }

        public BitSetQueryEnumerator(RenderComponentStore<T> store, BitSet bits)
        {
            _i = -1;
            _store = store;
            _length = store.Count;
            _bits = bits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _length)
            {
                var entity = _store.GetEntity(_i);
                if (_bits[entity.Id])
                {
                    Current = new RenderQueryItem(entity, ref _store.GetByIndex(_i));
                    return true;
                }
            }

            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly BitSetQueryEnumerator GetEnumerator() => this;
    }


    public ref struct SparseQueryEnumerator
    {
        private int _i;
        private readonly RenderComponentStore<T> _store;
        private readonly ReadOnlySpan<RenderEntity> _entities;

        public RenderQueryItem Current { get; private set; }

        public SparseQueryEnumerator(RenderComponentStore<T> store, ReadOnlySpan<RenderEntity> entities)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(entities.Length, store.Count);
            _i = -1;
            _store = store;
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
                    Current = new RenderQueryItem(entity, ref _store.Get(entity));
                    return true;
                }
            }

            return false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator GetEnumerator() => this;
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
}