using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public static class RenderEcs
{
    private const int DefaultRenderCap = 1024;

    public static readonly RenderEntityCore Core = new(DefaultRenderCap);
    //public static readonly FrameEntityStore Frame = new(DefaultRenderCap);

    private static readonly List<IRenderEntityStore> All = new(8);
    //private readonly List<Action<int>> _resizeCallbacks = [];

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
        if (RenderEntityStore<T>.Instance != null!)
            Throwers.InvalidArgument(nameof(T), "Store already initialized");

        RenderEntityStore<T>.Instance = new RenderEntityStore<T>(capacity);
        All.Add(RenderEntityStore<T>.Instance);
    }

    public static void Dispose()
    {
        foreach (var store in All) store.Dispose();
        Core.Dispose();
    }
}