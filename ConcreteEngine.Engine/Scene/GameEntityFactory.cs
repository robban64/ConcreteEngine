using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.Scene.Template;

namespace ConcreteEngine.Engine.Scene;

public static class GameEntityFactory
{
    internal static GameEntityId BuildGameEntity(SceneObject sceneObject, GameEntityHub entities, GameEntityTemplate e)
    {
        var entity = entities.AddEntity();
        if (e.Transform is { } transform)
            entities.AddComponent(entity, new TransformComponent(in transform.Transform));

        if (e.BoundingBox is { } bounds)
            entities.AddComponent(entity, new BoundingBoxComponent(in bounds.LocalBounds));

        if (e.Visibility is { } visibility)
            entities.AddComponent(entity, new VisibilityComponent { Visible = visibility.Enabled });

        foreach (var it in e.Components)
        {
            switch (it)
            {
                case AnimationTemplate animation:
                    entities.AddComponent(entity, new AnimationComponent
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