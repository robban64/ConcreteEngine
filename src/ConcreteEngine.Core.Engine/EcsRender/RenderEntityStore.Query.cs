using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.EcsRender;

public sealed unsafe partial class RenderEntityStore<T> where T : unmanaged, IRenderComponent<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentEnumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityQueryEnumerator VisibilityQuery() => new(_entities, _components, Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator SparseQuery(ReadOnlySpan<RenderEntity> entities) => new(entities);
    
    public readonly ref struct RenderQueryItem(RenderEntity entity, ref T component)
    {
        public readonly RenderEntity Entity = entity;
        public readonly ref T Component = ref component;
    }

    public ref struct ComponentEnumerator(RenderEntityStore<T> store)
    {
        private int _i = -1;
        private RenderEntity _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.IsValid)
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
        private RenderEntity* _entity;
        private T* _component;
        private readonly RenderEntity* _end;

        public VisibilityQueryEnumerator(RenderEntity* entities, T* component, int length)
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
        private readonly ReadOnlySpan<RenderEntity> _entities;
        private RenderEntity _entity;
        private int _i;

        public SparseQueryEnumerator(ReadOnlySpan<RenderEntity> entities)
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
                if (_entity.IsValid) return true;
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
            get => new(_entities[_i], ref RenderEcs.Store<T>().Get(_entities[_i]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly JoinQueryEnumerator GetEnumerator() => this;
    }
}