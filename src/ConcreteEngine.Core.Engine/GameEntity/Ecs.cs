using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.GameEntity.GameComponent;

namespace ConcreteEngine.Core.Engine.GameEntity;

public static partial class Ecs
{
    private const int DefaultGameCap = 32;

    public static GameEntityCore GameCore => Game.Core;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameEntityStore<T> GetGameStore<T>() where T : unmanaged, IGameComponent<T> => Game.Stores<T>.Store;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void InitGameEcs()
    {
        if (Game.StoreCount > 0)
            throw new InvalidOperationException("Ecs.Game already initialized");
/*
        Game.Stores<AnimationComponent>.CreateStore(DefaultGameCap);
        Game.Stores<RenderLink>.CreateStore(DefaultGameCap);
        Game.Stores<TransformComponent>.CreateStore(DefaultGameCap);
        Game.Stores<BoxComponent>.CreateStore(DefaultGameCap);
        Game.Stores<TagComponent>.CreateStore(DefaultGameCap);
*/
    }
}