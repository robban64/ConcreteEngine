using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Time;

namespace ConcreteEngine.Engine.Render;

public sealed class FrameProcessor
{
    internal void Execute(float delta, float alpha)
    {
        var renderAnimations = Ecs.Render.Stores<RenderAnimationComponent>.Store;
        foreach (var query in Ecs.Game.Query<AnimationComponent, RenderLink>())
        {
            var renderEntity = query.Component2.RenderEntityId;
            if (renderEntity == default) continue;

            var animationPtr = renderAnimations.TryGet(renderEntity);
            if (animationPtr.IsNull) continue;

            ref readonly var a = ref query.Component1;

            if (a.Time < a.PrevTime)
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);

            animationPtr.Value.Speed = a.Speed;
        }
    }

}