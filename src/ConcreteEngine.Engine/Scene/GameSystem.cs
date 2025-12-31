using ConcreteEngine.Engine.ECS.GameComponent;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Scene;

internal sealed class GameSystem()
{
    public void Update(float dt)
    {
        foreach (var query in Ecs.Game.Query<RenderLink, TransformComponent>())
        {
            var link = query.Component1;
            ref readonly var transform = ref query.Component2;
            Ecs.Render.Core.GetTransform(link.RenderEntityId).Transform = transform;
        }

        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }
}