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
    }

    private void CheckDirty()
    {
        foreach (var sceneObjectId in _store.GetDirtySpan())
        {
            var sceneObject = _store.Get(sceneObjectId);
            ref readonly var transform = ref sceneObject.GetTransform();

            MatrixMath.CreateModelMatrix(in transform, out var worldMatrix);
            foreach (var entity in sceneObject.GetRenderEntities())
            {
                var particleComp = Ecs.Render.Stores<ParticleComponent>.Store.TryGet(entity);
                if (!particleComp.IsNull)
                    _world.Particles.GetEmitter(particleComp.Value.Emitter).OriginTranslation = transform.Translation;

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