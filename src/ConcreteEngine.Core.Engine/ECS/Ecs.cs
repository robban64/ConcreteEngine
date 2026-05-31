using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.Integration;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    private const int DefaultRenderCap = 1024;
    private const int DefaultGameCap = 128;

    public static EntitySceneLink SceneLink { get; private set; } = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEntityStore<T> GetGameStore<T>() where T : unmanaged, IGameComponent<T> => Game.Stores<T>.Store;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderEntityStore<T> GetRenderStore<T>() where T : unmanaged, IRenderComponent<T> =>
        Render.Stores<T>.Store;

    internal static void Init()
    {
        if (SceneLink != null!) throw new InvalidOperationException("ECS already initialized");
        InitRenderEcs();
        InitGameEcs();
        SceneLink = new EntitySceneLink(Render.Core, Game.Core);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void InitRenderEcs()
    {
        if (Render.StoreCount > 0)
            throw new InvalidOperationException("Ecs.Render already initialized");

        Render.Stores<SkinningComponent>.CreateStore(16);
        Render.Stores<SkinLinkComponent>.CreateStore(64);
        Render.Stores<ParticleComponent>.CreateStore(16);
        Render.Stores<SelectionComponent>.CreateStore(16);
        Render.Stores<DebugBoundsComponent>.CreateStore(16);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void InitGameEcs()
    {
        if (Game.StoreCount > 0)
            throw new InvalidOperationException("Ecs.Game already initialized");

        Game.Stores<RenderLink>.CreateStore(DefaultGameCap);
        Game.Stores<TransformComponent>.CreateStore(DefaultGameCap);
        Game.Stores<BoxComponent>.CreateStore(DefaultGameCap);
        Game.Stores<AnimationComponent>.CreateStore(64);
        Game.Stores<TagComponent>.CreateStore(32);
    }
}