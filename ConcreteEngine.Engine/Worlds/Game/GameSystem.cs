using ConcreteEngine.Engine.ECS.GameComponent;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Game;

internal sealed class GameSystem()
{
    public void UpdateTick(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }

    }
}