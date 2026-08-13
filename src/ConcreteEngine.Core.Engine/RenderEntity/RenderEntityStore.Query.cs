using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityStore<T> where T : unmanaged, IRenderComponent<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentEnumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator VisibilityQuery() => new(_entities, _components, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator SparseQuery(ReadOnlySpan<RenderEntityId> entities) => new(entities);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public JoinQueryEnumerator JoinQuery(ReadOnlySpan<RenderEntityId> entities) => new(_entities, entities);

    public readonly ref struct RenderQueryItem(RenderEntityId entityId, ref T component)
    {
        public readonly RenderEntityId Entity = entityId;
        public readonly ref T Component = ref component;
    }

    public ref struct ComponentEnumerator(RenderEntityStore<T> store)
    {
        private int _i = -1;
        private RenderEntityId _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.IsValid())
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_currentEntity, ref store.GetByIndex(_i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ComponentEnumerator GetEnumerator() => this;
    }

    public ref struct VisibilityQueryEnumerator
    {
        private RenderEntityId* _entity;
        private T* _component;
        private readonly RenderEntityId* _end;

        public VisibilityQueryEnumerator(RenderEntityId* entities, T* component, int length)
        {
            _entity = entities - 1;
            _component = component - 1;
            _end = entities + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_entity < _end)
            {
                ++_component;
                if (RenderEcs.Core.IsVisible(*_entity)) return true;
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_entity, ref *_component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityQueryEnumerator GetEnumerator() => this;
    }


    public ref struct SparseQueryEnumerator
    {
        private readonly ReadOnlySpan<RenderEntityId> _entities;
        private RenderEntityId _entity;
        private int _i;

        public SparseQueryEnumerator(ReadOnlySpan<RenderEntityId> entities)
        {
            _entities = entities;
            _entity = default;
            _i = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _entities.Length)
            {
                _entity = _entities[_i];
                if (_entity.IsValid()) return true;
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entity, ref RenderEcs.Store<T>().Get(_entity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator GetEnumerator() => this;
    }


    public ref struct JoinQueryEnumerator
    {
        private readonly NativeView<RenderEntityId> _entities;
        private readonly ReadOnlySpan<RenderEntityId> _otherEntities;
        private int _i;

        public JoinQueryEnumerator(NativeView<RenderEntityId> entities, ReadOnlySpan<RenderEntityId> otherEntities)
        {
            _entities = entities;
            _otherEntities = otherEntities;
            _i = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _entities.Length)
            {
                var index = SearchMethod.BinarySearch(_otherEntities, _entities[_i]);
                if (index >= 0) return true;
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_entities[_i], ref RenderEcs.Store<T>().Get(_entities[_i]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly JoinQueryEnumerator GetEnumerator() => this;
    }
}