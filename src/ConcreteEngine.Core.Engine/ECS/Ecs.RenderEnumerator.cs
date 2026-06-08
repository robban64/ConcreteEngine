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
        public ref struct VisibleEntityEnumerator(NativeView<RenderEntity> entities)
        {
            private int _i = -1;
            private int _visibleIndex = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < entities.Length)
                {
                    if (!entities[_i].IsVisible()) continue;
                    ++_visibleIndex;
                    return true;
                }

                return false;
            }

            public readonly (int VisibleIndex, RenderEntityId Entity) Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => (_visibleIndex, new RenderEntityId(_i + 1));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleEntityEnumerator GetEnumerator() => new(entities);
        }


        public ref struct RenderEntityEnumerator(RenderEntityCore core)
        {
            private int _i = -1;
            private readonly int _count = core.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _count)
                {
                    if (core.Has(new RenderEntityId(_i + 1))) return true;
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
                    core.ToggleVisibilityFlag(Entity, flag, isVisible);

                public ref SourceComponent Source
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetSource(Entity);
                }

                public ref Transform Transform
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetTransform(Entity);
                }

                public ref BoundingBox Bounds
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetBounds(Entity);
                }

                public ref Matrix4x4 Matrix
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetMatrix(Entity);
                }
            }
        }
    }

    public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
    {
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
                    if (entity.Id <= 0 || !core.IsVisible(entity)) continue;
                    _currentEntity = entity;
                    return true;
                }

                return false;
            }

            public readonly QueryItem Current
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
                    if (!entity.IsValid()) continue;
                    _currentEntity = entity;
                    return true;
                }

                return false;
            }

            public readonly QueryItem Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(_i, _currentEntity, ref store.GetByIndex(_i));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly QueryEnumerator GetEnumerator() => new(store);
        }

        public readonly ref struct QueryItem(int idx, RenderEntityId entityId, ref T1 component)
        {
            public readonly int Index = idx;
            public readonly RenderEntityId Entity = entityId;
            public readonly ref T1 Component = ref component;
        }
    }
}