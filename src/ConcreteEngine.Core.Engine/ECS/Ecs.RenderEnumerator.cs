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
        public ref struct RenderEntityEnumerator(RenderEntityStore<T1> store)
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
    
    /*
        public readonly ref struct RenderCoreQuery<T1, T2>(ref T1 item1, ref T2 item2, RenderEntityId entity)
            where T1 : unmanaged where T2 : unmanaged
        {
            public readonly RenderEntityId Entity = entity;
            public readonly ref T1 Item1 = ref item1;
            public readonly ref T2 Item2 = ref item2;
        }

        public ref struct RenderCoreEnumerator<T1, T2>(RenderEntityCore core) where T1 : unmanaged where T2 : unmanaged
        {
            private int _i = -1;
            private RenderEntityId _entity;
            private readonly int _count = core.Count;
            public readonly ref T1 Item1 = ref RenderEntityCore.Store<T1>.Entries[0];
            public readonly ref T2 Item2 = ref RenderEntityCore.Store<T2>.Entries[0];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _count)
                {
                    if (core.Has(_entity = new RenderEntityId(_i + 1))) return true;
                }

                return false;
            }

            public readonly RenderCoreQuery<T1, T2> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new(ref Unsafe.Add(ref Item1, _i), ref Unsafe.Add(ref Item2, _i), _entity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RenderCoreEnumerator<T1, T2> GetEnumerator()
            {
                _i = -1;
                return this;
            }
        }
*/
}