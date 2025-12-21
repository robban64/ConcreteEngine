using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.GameComponent;

namespace ConcreteEngine.Engine.ECS;

public static class GameQuery<T1> where T1 : unmanaged, IGameComponent<T1>
{
    public ref struct EntityEnumerator(GameEntityStore<T1> r)
    {
        private int _i = -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < r.Count;

        public Item Current => new(_i, r);

        public readonly ref struct Item(int idx, GameEntityStore<T1> r)
        {
            public readonly int Index = idx;
            public GameEntityId Entity => r.GetEntity(Index);
            public ref T1 Component => ref r.GetByIndex(Index);
        }

        public EntityEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }
}


public static class GameQuery<T1, T2> where T1 : unmanaged, IGameComponent<T1> where T2 : unmanaged, IGameComponent<T2>
{
    public ref struct EntityEnumerator(GameEntityStore<T1> r1, GameEntityStore<T2> r2)
    {
        private int _i = -1;
        private GameEntityId _currentEntity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < r1.Count)
            {
                var entity = r1.GetEntity(_i);
                if (r2.Has(entity))
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public Item Current => new(_i, _currentEntity, r1, r2);

        public readonly ref struct Item(int idx, GameEntityId entity, GameEntityStore<T1> r1, GameEntityStore<T2> r2)
        {
            public readonly int Index = idx;
            public readonly GameEntityId Entity = entity;
            public ref T1 Component1 => ref r1.GetByIndex(Index);
            public ref T2 Component2 => ref r2.Get(Entity);
        }

        public EntityEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }
}