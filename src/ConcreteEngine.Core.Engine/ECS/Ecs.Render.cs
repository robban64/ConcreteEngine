using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class Render
    {
        private static readonly List<IRenderEntityStore> All = new(8);
        public static readonly RenderEntityCore Core = new(DefaultRenderCap);

        public static class Stores<T> where T : unmanaged, IRenderComponent<T>
        {
            public static RenderEntityStore<T> Store = null!;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void CreateStore(int cap)
            {
                if (Store != null) throw new InvalidOperationException("Ecs.Render - Store already created");
                var store = new RenderEntityStore<T>(cap);
                All.Add(store);
                Store = store;
            }
        }

        public static int EntityCount => Core.Count;
        public static int ActiveCount => Core.ActiveCount;
        public static int StoreCount => All.Count;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderQuery.RenderEntityEnumerator CoreQuery() => Core.Query();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderQuery<T1>.RenderEntityEnumerator Query<T1>() where T1 : unmanaged, IRenderComponent<T1> =>
            Stores<T1>.Store.Query();
    }
}