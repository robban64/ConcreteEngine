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
        public unsafe ref struct VisibleCoreEnumerator(RenderEntity* entities, int count)
        {
            private int _i = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < count)
                {
                    if (entities[_i].IsVisible()) return true;
                }
                return false;
            }

            public readonly RenderEntityId Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new (_i + 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly VisibleCoreEnumerator GetEnumerator() => new(entities, count);
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
        public readonly ref struct RenderQueryItem(int idx, RenderEntityId entityId, ref T1 component)
        {
            public readonly ref T1 Component = ref component;
            public readonly int Index = idx;
            public readonly RenderEntityId Entity = entityId;
        }


    }

}