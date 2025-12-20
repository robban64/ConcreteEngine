using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Time;

namespace ConcreteEngine.Engine.Worlds.Render;

public sealed class WorldRenderer
{
    private readonly GameEntityHub _gameEcs;
    private readonly RenderEntityHub _renderEcs;
    private readonly DrawEntityPipeline _drawEntities;
    private readonly Camera _camera;

    internal WorldRenderer(GameEntityHub gameEcs, RenderEntityHub renderEcs, DrawEntityPipeline drawEntities, Camera camera)
    {
        _gameEcs = gameEcs;
        _renderEcs = renderEcs;
        _drawEntities = drawEntities;
        _camera = camera;
    }

    internal void BeforeRender()
    {
        var gameEcs = _gameEcs;
        var renderEcs = _renderEcs;
        var alpha = EngineTime.GameAlpha;
        var dt = EngineTime.DeltaTime;

        var renderAnimations = renderEcs.GetStore<RenderAnimationComponent>();
        foreach (var query in gameEcs.Query<AnimationComponent, RenderLink>())
        {
            var renderEntity = query.Component2.RenderEntityId;;
            if(renderEntity == default) continue;

            var animationPtr = renderAnimations.TryGet(renderEntity);
            if(animationPtr.IsNull) continue;

            ref readonly var a = ref query.Component1;

            if (a.Time < a.PrevTime)
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time + a.Duration, alpha) % a.Duration;
            else 
                animationPtr.Value.Time = float.Lerp(a.PrevTime, a.Time, alpha);

            animationPtr.Value.Speed = a.Speed;
        }

    }


}