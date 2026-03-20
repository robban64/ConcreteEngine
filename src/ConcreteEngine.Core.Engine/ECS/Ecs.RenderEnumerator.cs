using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class RenderQuery
    {
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

            public RenderEntityEnumerator GetEnumerator()
            {
                _i = -1;
                return this;
            }

            public readonly ref struct EntityCoreQuery(RenderEntityCore core, RenderEntityId entity)
            {
                public readonly RenderEntityId Entity = entity;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public VisibilityFlags ToggleVisibilityFlag(VisibilityFlags flag, bool isVisible)
                    => core.ToggleVisibilityFlag(Entity, flag, isVisible);

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

                public ref BoundingBox Box
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetBounds(Entity);
                }

                public ref Matrix4x4 Parent
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref core.GetParentMatrix(Entity);
                }
            }
        }
    }

    public static class RenderQuery<T1> where T1 : unmanaged, IRenderComponent<T1>
    {
        public ref struct RenderEntityEnumerator( RenderEntityStore<T1> store)
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

            public readonly Item Current => new(store, _i, _currentEntity);

            public readonly ref struct Item(RenderEntityStore<T1> store, int idx, RenderEntityId entityId)
            {
                public readonly int Index = idx;
                public readonly RenderEntityId Entity = entityId;

                public ref T1 Component
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => ref store.GetByIndex(Index);
                }
                
            }

            public RenderEntityEnumerator GetEnumerator()
            {
                _i = -1;
                return this;
            }
        }
    }
}