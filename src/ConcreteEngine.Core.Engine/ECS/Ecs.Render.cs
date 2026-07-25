using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    public static class Render
    {
        private static readonly List<IRenderEntityStore> All = new(8);
        public static readonly RenderEntityCore Core = new(DefaultRenderCap);

        public static int EntityCount => Core.Count;
        public static int ActiveCount => Core.ActiveCount;
        public static int StoreCount => All.Count;

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
    }
}