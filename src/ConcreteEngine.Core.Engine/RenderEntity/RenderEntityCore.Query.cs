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
    public QueryEnumerator<BoundingBox> BoundsQuery(EntityVisibility skipFlag = EntityVisibility.ForceHidden) => new(_meta, _bounds, Count, skipFlag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibleCoreEnumerator VisibilityQuery() => new(this, _meta, Count);


    //
    public ref struct QueryEnumerator<T> where T : unmanaged
    {
        private T* _p1;
        private RenderEntityMeta* _entities;
        private readonly RenderEntityMeta* _end;
        private int _entity;
        private readonly EntityVisibility _skipFlag;

        public QueryEnumerator(RenderEntityMeta* entities, T* p1, int length, EntityVisibility skipFlag )
        {
            _skipFlag = skipFlag;
            _entity = 0;
            _entities = entities - 1;
            _p1 = p1 - 1;
            _end = entities + length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_entities < _end)
            {
                ++_p1;
                ++_entity;
                if (_entities->Alive && _entities->Visibility != _skipFlag) return true;
            }

            return false;
        }

        public readonly QueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(new RenderEntityId(_entity), ref *_entities, ref *_p1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly QueryEnumerator<T> GetEnumerator() => this;

        public readonly ref struct QueryItem(RenderEntityId entity, ref RenderEntityMeta meta, ref T p1)
        {
            public readonly RenderEntityId Entity = entity;
            public readonly ref RenderEntityMeta Meta = ref meta;
            public readonly ref T Item = ref p1;
        }
    }

    //
    public unsafe ref struct VisibleCoreEnumerator
    {
        private RenderEntityMeta* _current;
        private RenderEntityMeta* _end;
        private int _entityId;
        private readonly RenderEntityCore _core;

        public VisibleCoreEnumerator(RenderEntityCore core, RenderEntityMeta* entities, int count)
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
                if (_current->IsVisible()) return true;
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

        public readonly ref struct Item(RenderEntityId entity, ref RenderEntityMeta meta, RenderEntityCore core)
        {
            public readonly RenderEntityId Entity = entity;
            public readonly ref RenderEntityMeta Meta = ref meta;

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