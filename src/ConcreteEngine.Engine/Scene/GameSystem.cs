using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS.GameComponent;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Scene;

internal sealed class GameSystem(SceneManager sceneManager)
{
    private readonly SceneManager _sceneManager = sceneManager;
    private readonly SceneStore _store = sceneManager.Store;

    public void Update(float dt)
    {
        foreach (var sceneObjectId in _store.GetDirtySpan())
        {
            var sceneObject = _store.Get(sceneObjectId);
            MatrixMath.CreateModelMatrix(in sceneObject.GetTransform(), out var world);
            foreach (var entity in sceneObject.GetRenderEntities())
            {
                Ecs.Render.Core.GetParentMatrix(entity).World = world;
            }
        }
        
        _store.ClearDirty();
        /*
        foreach (var query in Ecs.Game.Query<RenderLink, TransformComponent>())
        {
            var link = query.Component1;
            ref readonly var transform = ref query.Component2;
            Ecs.Render.Core.GetTransform(link.RenderEntityId).Transform = transform;
        }
        */

        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }
}