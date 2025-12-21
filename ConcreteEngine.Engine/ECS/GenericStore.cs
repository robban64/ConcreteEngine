using System.Runtime.InteropServices;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

internal static class GenericStore
{
    private static readonly List<IRenderEntityStore> RenderStores = new(8);
    private static readonly List<IGameEntityStore> GameStores = new(8);

    public static int RenderStoreCount => RenderStores.Count;
    public static int GameStoreCount => GameStores.Count;

    public static RenderEntityCore CoreStore = null!;

    public static class Render<T> where T : unmanaged, IRenderComponent<T>
    {
        public static RenderEntityStore<T> Store = null!;

        public static void CreateStore(int cap)
        {
            var store = new RenderEntityStore<T>(cap);
            RenderStores.Add(store);
            Store = store;
        }
    }

    public static class Game<T> where T : unmanaged, IGameComponent<T>
    {
        public static GameEntityStore<T> Store = null!;

        public static void CreateStore(int cap)
        {
            var store = new GameEntityStore<T>(cap);
            GameStores.Add(store);
            Store = store;
        }
    }
}