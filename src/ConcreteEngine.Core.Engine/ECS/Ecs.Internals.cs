using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS;

public static partial class Ecs
{
    internal static class Internals
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Warmup()
        {
            for (var i = 0; i < 30; i++)
            {
                _ = Render.Core.Count;
                _ = Render.Core.Capacity;
                _ = Render.Core.Has(new RenderEntityId(1));
                _ = Render.Core.GetBounds(new RenderEntityId(1));
                _ = Render.Core.GetSource(new RenderEntityId(1));
                _ = Render.Core.GetTransform(new RenderEntityId(1));
                _ = Render.Core.GetParentMatrix(new RenderEntityId(1));
                _ = Render.Core.IsVisible(new RenderEntityId(1));

                _ = Render.Stores<RenderAnimationComponent>.Store.TryGet(new RenderEntityId(1));
                _ = Render.Stores<ParticleComponent>.Store.TryGet(new RenderEntityId(1));
                _ = Render.Stores<SelectionComponent>.Store.TryGet(new RenderEntityId(1));
                _ = Render.Stores<DebugBoundsComponent>.Store.TryGet(new RenderEntityId(1));

                _ = Render.Stores<RenderAnimationComponent>.Store.Has(new RenderEntityId(1));
                _ = Render.Stores<ParticleComponent>.Store.Has(new RenderEntityId(1));
                _ = Render.Stores<SelectionComponent>.Store.Has(new RenderEntityId(1));
                _ = Render.Stores<DebugBoundsComponent>.Store.Has(new RenderEntityId(1));

                _ = Game.Core.Has(new GameEntityId(1));
                _ = Game.Core.Count;
                _ = Game.Core.Capacity;

                _ = new GameEntityId(1).Index();
                _ = Game.Stores<RenderLink>.Store.Has(new GameEntityId(1));
                _ = Game.Stores<TransformComponent>.Store.Has(new GameEntityId(1));
                _ = Game.Stores<BoxComponent>.Store.Has(new GameEntityId(1));
                _ = Game.Stores<AnimationComponent>.Store.Has(new GameEntityId(1));
                _ = Game.Stores<TagComponent>.Store.Has(new GameEntityId(1));
                _ = Game.Stores<ParticleRefComponent>.Store.Has(new GameEntityId(1));

                _ = Game.Stores<RenderLink>.Store.TryGet(new GameEntityId(1));
                _ = Game.Stores<TransformComponent>.Store.TryGet(new GameEntityId(1));
                _ = Game.Stores<BoxComponent>.Store.TryGet(new GameEntityId(1));
                _ = Game.Stores<AnimationComponent>.Store.TryGet(new GameEntityId(1));
                _ = Game.Stores<TagComponent>.Store.TryGet(new GameEntityId(1));
                _ = Game.Stores<ParticleRefComponent>.Store.TryGet(new GameEntityId(1));
            }
        }
    }
}