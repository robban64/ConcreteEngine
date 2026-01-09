using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Scene;

internal sealed class GameSystem(SceneManager sceneManager, World world)
{
    private readonly SceneManager _sceneManager = sceneManager;
    private readonly SceneStore _store = sceneManager.Store;

    private readonly RenderEntityCore _renderEcs = Ecs.Render.Core;
    private readonly GameEntityCore _gameEcs = Ecs.Game.Core;

    private readonly World _world = world;

    public void Update(float dt)
    {
        CheckDirty();
        UpdateAnimations(dt);


        /*
 foreach (var query in Ecs.Game.Query<RenderLink, TransformComponent>())
 {
     var link = query.Component1;
     ref readonly var transform = ref query.Component2;
     Ecs.Render.Core.GetTransform(link.RenderEntityId).Transform = transform;
 }
 */
    }

    private void CheckDirty()
    {
        foreach (var sceneObjectId in _store.GetDirtySpan())
        {
            var sceneObject = _store.Get(sceneObjectId);

            MatrixMath.CreateModelMatrix(in sceneObject.GetTransform(), out var worldMatrix);
            foreach (var entity in sceneObject.GetRenderEntities())
            {
                var particleComp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (!particleComp.IsNull)
                {
                    _world.Particles.GetEmitter(particleComp.Value.Emitter).OriginTranslation =
                        sceneObject.GetTransform().Translation;
                }

                _renderEcs.GetParentMatrix(entity).World = worldMatrix;
            }
        }

        _store.ClearDirty();
    }

    private void UpdateAnimations(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }
}