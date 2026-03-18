using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.GameComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class Game
    {
        private static readonly List<IGameEntityStore> All = new(8);
        public static readonly GameEntityCore Core = new(DefaultGameCap);

        public static class Stores<T> where T : unmanaged, IGameComponent<T>
        {
            public static GameEntityStore<T> Store = null!;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void CreateStore(int cap)
            {
                if (Store != null) throw new InvalidOperationException("Ecs.Game - Store already created");
                var store = new GameEntityStore<T>(cap);
                All.Add(store);
                Store = store;
            }
        }

        public static int EntityCount => Core.Count;
        public static int ActiveCount => Core.ActiveCount;
        public static int StoreCount => All.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameQuery<T1>.EntityEnumerator Query<T1>() where T1 : unmanaged, IGameComponent<T1> =>
            new(Stores<T1>.Store);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GameQuery<T1, T2>.EntityEnumerator Query<T1, T2>()
            where T1 : unmanaged, IGameComponent<T1> where T2 : unmanaged, IGameComponent<T2> =>
            new(Stores<T1>.Store, Stores<T2>.Store);
    }
}