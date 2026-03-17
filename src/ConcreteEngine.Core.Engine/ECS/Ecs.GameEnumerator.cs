using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.GameComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class GameQuery<T1> where T1 : unmanaged, IGameComponent<T1>
    {
        public ref struct EntityEnumerator(GameEntityStore<T1> store)
        {
            private int _i = -1;
            private GameEntityId _currentEntity;
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

            public readonly Item Current => new(_i, _currentEntity, store);

            public readonly ref struct Item(int idx, GameEntityId entity, GameEntityStore<T1> store)
            {
                public readonly int Index = idx;
                public readonly GameEntityId Entity = entity;
                public ref T1 Component => ref store.GetByIndex(Index);
            }

            public EntityEnumerator GetEnumerator()
            {
                _i = -1;
                return this;
            }
        }
    }


    public static class GameQuery<T1, T2> where T1 : unmanaged, IGameComponent<T1>
        where T2 : unmanaged, IGameComponent<T2>
    {
        public ref struct EntityEnumerator(GameEntityStore<T1> store1, GameEntityStore<T2> store2)
        {
            private int _i = -1;
            private GameEntityId _currentEntity;
            private readonly int _count = store1.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_i < _count)
                {
                    var entity = store1.GetEntity(_i);
                    if (entity.IsValid() && store2.Has(entity))
                    {
                        _currentEntity = entity;
                        return true;
                    }
                }

                return false;
            }

            public readonly Item Current => new(_i, _currentEntity, store1, store2);

            public readonly ref struct Item(
                int idx,
                GameEntityId entity,
                GameEntityStore<T1> store1,
                GameEntityStore<T2> store2)
            {
                public readonly int Index = idx;
                public readonly GameEntityId Entity = entity;
                public ref T1 Component1 => ref store1.GetByIndex(Index);
                public ref T2 Component2 => ref store2.Get(Entity);
            }

            public EntityEnumerator GetEnumerator()
            {
                _i = -1;
                return this;
            }
        }
    }
}