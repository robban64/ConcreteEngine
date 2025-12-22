using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public static partial class Ecs
{
    public static class Render
    {
        private static readonly List<IRenderEntityStore> All = new(8);
        public static readonly RenderEntityCore Core = new(DefaultRenderCap);

        public static class Stores<T> where T : unmanaged, IRenderComponent<T>
        {
            public static RenderEntityStore<T> Store = null!;

            public static void CreateStore(int cap)
            {
                var store = new RenderEntityStore<T>(cap);
                All.Add(store);
                Store = store;
            }
        }

        public static int EntityCount => Core.Count;
        public static int ActiveCount => Core.ActiveCount;
        public static int StoreCount => All.Count;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderQuery.RenderEntityEnumerator CoreQuery() => new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderQuery<T1>.RenderEntityEnumerator Query<T1>() where T1 : unmanaged, IRenderComponent<T1> =>
            new();
    }
}