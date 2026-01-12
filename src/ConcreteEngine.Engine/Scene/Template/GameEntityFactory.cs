using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;

namespace ConcreteEngine.Engine.Scene.Template;

public static class GameEntityFactory
{
    internal static GameEntityId BuildGameEntity(SceneObject sceneObject, GameEntityTemplate e)
    {
        var entity = Ecs.Game.Core.AddEntity();
        if (e.Transform is { } transform)
            Ecs.Game.Stores<TransformComponent>.Store.Add(entity, new TransformComponent(in transform.Transform));

        if (e.BoundingBox is { } bounds)
            Ecs.Game.Stores<BoundingBoxComponent>.Store.Add(entity, new BoundingBoxComponent(in bounds.LocalBounds));

        if (e.Visibility is { } visibility)
            Ecs.Game.Stores<VisibilityComponent>.Store.Add(entity,
                new VisibilityComponent { Visible = visibility.Enabled });

        foreach (var it in e.Components)
        {
            switch (it)
            {
                case AnimationTemplate animation:
                    Ecs.Game.Stores<AnimationComponent>.Store.Add(entity, new AnimationComponent
                    {
                        Clip = animation.Clip,
                        Duration = animation.Duration,
                        Speed = animation.Speed,
                        State = animation.State,
                        Time = animation.Time,
                    });
                    break;
            }
        }

        sceneObject.AddGameEntity(entity);
        return entity;
    }
}