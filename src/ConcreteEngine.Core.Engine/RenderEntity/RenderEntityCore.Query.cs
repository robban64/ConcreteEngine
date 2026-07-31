using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed unsafe partial class RenderEntityCore
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<BoundingBox> CullQuery(EntityStatus skipFlag = EntityStatus.ForceHidden) =>
        new(_headers, _bounds, Count, skipFlag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator VisibilityQuery() => new(this, _headers, Count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator<DrawPolicy, Matrix4x4> DepthPolicyQuery(NativeView<RenderEntityId> entities) =>
        new(entities, _policies, _models, entities.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseQueryEnumerator<Matrix4x4, Matrix3X4> MatrixQuery(NativeView<RenderEntityId> entities) =>
        new(entities, _models, _normals, entities.Length);

    //
    public readonly ref struct QueryItem<T1>(RenderEntityId entity, ref T1 item1) where T1 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
    }

    public readonly ref struct QueryItem<T1, T2>(RenderEntityId entity, ref T1 item1, ref T2 item2)
        where T1 : unmanaged where T2 : unmanaged
    {
        public readonly RenderEntityId Entity = entity;
        public readonly ref T1 Item1 = ref item1;
        public readonly ref T2 Item2 = ref item2;
    }

    public ref struct QueryEnumerator<T> where T : unmanaged
    {
        private T* _p1;
        private EntityHeader* _current;
        private readonly EntityHeader* _end;
        private int _entity;
        private readonly EntityStatus _skipStatus;

        public QueryEnumerator(EntityHeader* current, T* p1, int length, EntityStatus skipStatus)
        {
            _skipStatus = skipStatus;
            _entity = 0;
            _current = current - 1;
            _p1 = p1 - 1;
            _end = current + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_current < _end)
            {
                ++_p1;
                ++_entity;
                if (_current->Status != 0 && _current->Status != _skipStatus) return true;
            }

            return false;
        }

        public readonly QueryItem<EntityHeader, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_entity), ref *_current, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly QueryEnumerator<T> GetEnumerator() => this;

    }

    public ref struct SparseQueryEnumerator<T> where T : unmanaged
    {
        private readonly T* _p1;
        private RenderEntityId* _current;
        private readonly RenderEntityId* _end;

        public SparseQueryEnumerator(RenderEntityId* current, T* p1, int length)
        {
            _p1 = p1;
            _current = current - 1;
            _end = current + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly QueryItem<T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_current, ref _p1[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T> GetEnumerator() => this;
    }

    public ref struct SparseQueryEnumerator<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        private readonly T1* _p1;
        private readonly T2* _p2;

        private RenderEntityId* _current;
        private readonly RenderEntityId* _end;

        public SparseQueryEnumerator(RenderEntityId* current, T1* p1, T2* p2, int length)
        {
            _p1 = p1;
            _p2 = p2;
            _current = current - 1;
            _end = current + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_current < _end;

        public readonly QueryItem<T1, T2> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(*_current, ref _p1[_current->Index()], ref _p2[_current->Index()]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SparseQueryEnumerator<T1, T2> GetEnumerator() => this;
    }
    
    //
    public ref struct VisibleCoreEnumerator
    {
        private EntityHeader* _current;
        private readonly EntityHeader* _end;
        private int _entityId;
        private readonly RenderEntityCore _core;

        public VisibleCoreEnumerator(RenderEntityCore core, EntityHeader* entities, int count)
        {
            _current = entities - 1;
            _entityId = 0;
            _end = entities + count;
            _core = core;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ++_entityId;
            while (++_current < _end)
            {
                if (_current->Status >= EntityStatus.Normal) return true;
            }

            return false;
        }

        public readonly Item Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_entityId), ref *_current, _core);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibleCoreEnumerator GetEnumerator() => this;

        public readonly ref struct Item(RenderEntityId entity, ref EntityHeader meta, RenderEntityCore core)
        {
            public readonly RenderEntityId Entity = entity;
            public readonly ref EntityHeader Status = ref meta;

            public ref RenderSource Source
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref core.GetSource(Entity);
            }

            public ref Matrix4x4 Model
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref core.GetModelMatrix(Entity);
            }

            public ref BoundingBox Bounds
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref core.GetWorldBounds(Entity);
            }
        }
    }
}