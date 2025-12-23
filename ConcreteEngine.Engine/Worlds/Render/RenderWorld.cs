using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render;

public sealed class RenderWorld
{
    private readonly RenderContext _ctx;
    private readonly GameEntityCore _gameEcs;
    private readonly Camera _camera;

    private readonly DrawEntityPipeline _drawEntities;

    internal DrawEntityPipeline DrawEntityPipeline => _drawEntities;

    internal RenderWorld(RenderContext ctx)
    {
        _drawEntities = new DrawEntityPipeline();
        _ctx = ctx;
        _camera = ctx.Camera;
    }

    internal void BeforeRender()
    {
        var alpha = EngineTime.GameAlpha;
        var dt = EngineTime.DeltaTime;

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

    internal void Execute(World world, DrawCommandBuffer commandBuffer)
    {
        _drawEntities.Reset();
        DrawEntityPipeline.ExecuteWorldObjects(commandBuffer, world);
        _drawEntities.Execute(_ctx, commandBuffer);
    }
}