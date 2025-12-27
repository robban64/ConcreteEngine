using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS;

public static partial class Ecs
{
    private const int DefaultRenderCap = 1024;
    private const int DefaultGameCap = 128;

    public static void InitRenderEcs()
    {
        if (Render.StoreCount > 0)
            throw new InvalidOperationException("Ecs.Render already initialized");

        Render.Core.Initialize();
        Render.Stores<RenderAnimationComponent>.CreateStore(64);
        Render.Stores<ParticleComponent>.CreateStore(16);
        Render.Stores<SelectionComponent>.CreateStore(16);
        Render.Stores<DebugBoundsComponent>.CreateStore(16);
    }

    public static void InitGameEcs()
    {
        if (Game.StoreCount > 0)
            throw new InvalidOperationException("Ecs.Game already initialized");

        Game.Core.Initialize();

        Game.Stores<RenderLink>.CreateStore(DefaultGameCap);
        Game.Stores<VisibilityComponent>.CreateStore(DefaultGameCap);
        Game.Stores<TransformComponent>.CreateStore(DefaultGameCap);
        Game.Stores<BoundingBoxComponent>.CreateStore(DefaultGameCap);
        Game.Stores<AnimationComponent>.CreateStore(64);
        Game.Stores<TagComponent>.CreateStore(32);
        Game.Stores<ParticleRefComponent>.CreateStore(32);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Warmup()
    {
        foreach (var it in Render.CoreQuery())
        {
            if (it.Index > 30) return;

            _ = it.RenderEntity;
            _ = it.Source;
            _ = it.Box;
            _ = it.Transform;
            _ = it.TryGetSpatial();

            _ = Render.Stores<RenderAnimationComponent>.Store.TryGet(new RenderEntityId(1));
            _ = Render.Stores<ParticleComponent>.Store.TryGet(new RenderEntityId(1));
            _ = Render.Stores<SelectionComponent>.Store.TryGet(new RenderEntityId(1));
            _ = Render.Stores<DebugBoundsComponent>.Store.TryGet(new RenderEntityId(1));

            _ = Render.Stores<RenderAnimationComponent>.Store.Has(new RenderEntityId(1));
            _ = Render.Stores<ParticleComponent>.Store.Has(new RenderEntityId(1));
            _ = Render.Stores<SelectionComponent>.Store.Has(new RenderEntityId(1));
            _ = Render.Stores<DebugBoundsComponent>.Store.Has(new RenderEntityId(1));

            _ = Game.Core.Has(new GameEntityId(1, 0));
            _ = Game.Core.Count;

            _ = Game.Stores<RenderLink>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<VisibilityComponent>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<TransformComponent>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<BoundingBoxComponent>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<AnimationComponent>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<TagComponent>.Store.Has(new GameEntityId(1, 0));
            _ = Game.Stores<ParticleRefComponent>.Store.Has(new GameEntityId(1, 0));

            _ = Game.Stores<RenderLink>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<VisibilityComponent>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<TransformComponent>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<BoundingBoxComponent>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<AnimationComponent>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<TagComponent>.Store.GetByIndex(new GameEntityId(1, 0));
            _ = Game.Stores<ParticleRefComponent>.Store.GetByIndex(new GameEntityId(1, 0));
        }
    }
}