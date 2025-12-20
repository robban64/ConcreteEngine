using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;

namespace ConcreteEngine.Engine.Worlds.Game;

internal sealed class GameSystem(GameEntityHub gameEcs)
{
    public void UpdateTick(float dt)
    {
        foreach (var query in gameEcs.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }

    }
}