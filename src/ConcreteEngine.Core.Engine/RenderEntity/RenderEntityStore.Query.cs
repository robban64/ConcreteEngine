using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityStore<T> where T : unmanaged, IRenderComponent<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator SparseQuery(NativeView<RenderEntityId> entities) => new(entities);

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public JoinQueryEnumerator JoinQuery(ReadOnlySpan<RenderEntityId> entities) => new(_entities, entities);

    public readonly ref struct RenderQueryItem(RenderEntityId entityId, ref T component)
    {
        public readonly RenderEntityId Entity = entityId;
        public readonly ref T Component = ref component;
    }

    public ref struct Enumerator(RenderEntityStore<T> store)
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
        public readonly Enumerator GetEnumerator() => this;
    }


    public ref struct SparseQueryEnumerator
    {
        private RenderEntityId* _entity;
        private readonly RenderEntityId* _end;
        private int _index;

        public SparseQueryEnumerator(NativeView<RenderEntityId> entities)
        {
            _index = 0;
            _entity = entities.Ptr - 1;
            _end = entities.Ptr + entities.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_entity < _end)
            {
                var index = RenderEcs.Store<T>().FindIndexSorted(*_entity);
                if (index >= 0)
                {
                    _index = index;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_entity, ref RenderEcs.Store<T>().GetByIndex(_index));
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