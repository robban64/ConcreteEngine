using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public static class RenderEcs
{
    private const int DefaultRenderCap = 1024;

    public static readonly RenderEntityCore Core = new(DefaultRenderCap);
    public static EntitySceneLink SceneLink { get; private set; } = null!;

    private static readonly List<IRenderEntityStore> All = new(8);

    public static int EntityCount => Core.Count;
    public static int ActiveCount => Core.ActiveCount;
    public static int StoreCount => All.Count;
    

    internal static void Init()
    {
        if (SceneLink != null!) throw new InvalidOperationException("ECS already initialized");
        Stores<SkinningLink>.CreateStore(16);
        Stores<EmitterLink>.CreateStore(16);
        Stores<SelectionComponent>.CreateStore(16);
        Stores<DebugBoundsComponent>.CreateStore(16);
        SceneLink = new EntitySceneLink(Core);
    }

    public static void Dispose()
    {
        foreach (var store in All) store.Dispose();
        Core.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntityStore<T> Store<T>() where T : unmanaged, IRenderComponent<T> =>
        Stores<T>.Store;

    private static class Stores<T> where T : unmanaged, IRenderComponent<T>
    {
        public static RenderEntityStore<T> Store = null!;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CreateStore(int cap)
        {
            if (Store != null!) Throwers.InvalidOperation("Ecs.Render - Store already created");
            var store = new RenderEntityStore<T>(cap);
            All.Add(store);
            Store = store;
        }
    }

}