using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.Render.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public  static partial class RenderEcs
{
    private const int DefaultRenderCap = 1024;

    public static readonly RenderEntityCore Core = new(DefaultRenderCap);
    private static readonly List<IRenderEntityStore> All = new(8);

    public static int EntityCount => Core.Count;
    public static int ActiveCount => Core.ActiveCount;
    public static int StoreCount => All.Count;

    internal static void OnResize(int newSize)
    {
    }

    internal static void Init()
    {
        if (All.Count > 0) throw new InvalidOperationException("ECS already initialized");
        CreateStore<DrawInstancedComponent>(32);
        CreateStore<SkinningLink>(16);
        CreateStore<EmitterLink>(16);
        CreateStore<SelectionComponent>(16);
        CreateStore<DebugBoundsComponent>(16);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntityStore<T> Store<T>() where T : unmanaged, IRenderComponent<T> =>
        RenderEntityStore<T>.Instance;

    private static void CreateStore<T>(int capacity) where T : unmanaged, IRenderComponent<T>
    {
        var store = new RenderEntityStore<T>(capacity);
        All.Add(store);
    }

    public static void Dispose()
    {
        foreach (var store in All) store.Dispose();
        Core.Dispose();
    }
}