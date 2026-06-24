using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class RenderQuery
    {
        public readonly ref struct RenderQueryContextItem(RenderEntityId entity, RenderEntityCore core)
        {
            public readonly RenderEntityId Entity = entity;

            public ref RenderEntity EntityCore => ref core.GetCoreEntity(Entity);

            public ref SourceComponent Source
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref core.GetSource(Entity);
            }

            public ref Matrix4x4 Matrix
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

        public ref struct VisibleCoreEnumerator(RenderEntityCore core)
        {
            private int _i = -1;
            private readonly NativeView<RenderEntity> _entities = core.GetCoreEntityView();
            private readonly RenderEntityCore _core = core;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _entities.Length)
                {
                    if (_entities[_i].IsVisible()) return true;
                }

                return false;
            }

            public readonly RenderQueryContextItem Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(new RenderEntityId(_i + 1), _core);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleCoreEnumerator GetEnumerator() => new(_core);
        }

        public ref struct VisibleCoreEnumerator<T>(NativeView<RenderEntity> entities, NativeView<T> data)
            where T : unmanaged
        {
            private int _i = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < entities.Length)
                {
                    if (entities[_i].IsVisible()) return true;
                }

                return false;
            }

            public readonly RenderQueryItem<ValuePtr<T>> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_i, new ValuePtr<T>(ref data[_i]));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleCoreEnumerator<T> GetEnumerator() => new(entities, data);
        }

        public ref struct VisibleCoreEnumerator<T1, T2>(
            NativeView<RenderEntity> entities,
            NativeView<T1> data1,
            NativeView<T2> data2) where T1 : unmanaged where T2 : unmanaged
        {
            private int _i = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < entities.Length)
                {
                    if (entities[_i].IsVisible()) return true;
                }

                return false;
            }

            public readonly RenderQueryItem<TuplePtr<T1, T2>> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_i, new TuplePtr<T1, T2>(ref data1[_i], ref data2[_i]));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleCoreEnumerator<T1, T2> GetEnumerator() => new(entities, data1, data2);
        }

        public readonly ref struct RenderQueryItem<T>(int index, T data) where T : allows ref struct
        {
            public readonly T Data = data;
            public readonly RenderEntityId Entity = new(index + 1);
            public readonly int Index = index;
        }
    }

    public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
    {
        public readonly ref struct RenderQueryItem(int idx, RenderEntityId entityId, ref T1 component)
        {
            public readonly ref T1 Component = ref component;
            public readonly int Index = idx;
            public readonly RenderEntityId Entity = entityId;
        }

        public ref struct VisibleQueryEnumerator(RenderEntityStore<T1> store, RenderEntityCore core)
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
                    if (entity.Id > 0 && core.IsVisible(entity))
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
                get => new(_i, _currentEntity, ref store.GetByIndex(_i));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleQueryEnumerator GetEnumerator() => new(store, core);
        }

        public ref struct QueryEnumerator(RenderEntityStore<T1> store)
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
                get => new(_i, _currentEntity, ref store.GetByIndex(_i));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly QueryEnumerator GetEnumerator() => new(store);
        }
    }
}

/*
   public ref struct RenderEntityEnumerator(RenderEntityCore core)
        {
            private int _i = -1;
            private readonly int _count = core.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _count)
                {
                    if (core.IsAlive(new RenderEntityId(_i + 1))) return true;
                }

                return false;
            }

            public readonly EntityCoreQuery Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(core, new RenderEntityId(_i + 1));
            }

            public RenderEntityEnumerator GetEnumerator() => new(core);

            public readonly ref struct EntityCoreQuery(RenderEntityCore core, RenderEntityId entity)
            {
                public readonly RenderEntityId Entity = entity;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public VisibilityFlags ToggleVisibilityFlag(VisibilityFlags flag, bool isVisible) =>
                    core.ToggleVisibility(Entity, flag, isVisible);

                public ref SourceComponent Source
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetSource(Entity);
                }

                public ref Transform Transform
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetLocalTransform(Entity);
                }

                public ref BoundingBox Bounds
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetWorldBounds(Entity);
                }

                public ref Matrix4x4 Matrix
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetWorldMatrix(Entity);
                }
            }
        }
*/