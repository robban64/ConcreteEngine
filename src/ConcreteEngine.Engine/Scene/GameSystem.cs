using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.GameComponent;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Scene;

internal sealed class GameSystem(AssetStore assetStore, SceneManager sceneManager, World world)
{
    private readonly SceneManager _sceneManager = sceneManager;
    private readonly SceneStore _store = sceneManager.Store;

    private readonly RenderEntityCore _renderEcs = Ecs.Render.Core;
    private readonly GameEntityCore _gameEcs = Ecs.Game.Core;

    public void Update(float dt)
    {
        CheckDirty();
        UpdateAnimations(dt);
    }


    private void UpdateAnimations(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }

    //Wip
    private void CheckDirty()
    {
        var dirtySpan = _store.GetDirtySpan();
        var particles = world.Particles;

        var renderEcs = _renderEcs;
        var particleEcs = Ecs.Render.Stores<ParticleComponent>.Store;
        var animationEcs = Ecs.Render.Stores<RenderAnimationComponent>.Store;

        var worldMatrix = Matrix4x4.Identity;
        foreach (var sceneObjectId in dirtySpan)
        {
            var sceneObject = _store.Get(sceneObjectId);

            ref readonly var transform = ref sceneObject.GetTransform();

            MatrixMath.CreateModelMatrix(in transform, out var rootMatrix);
            foreach (var entity in sceneObject.GetRenderEntities())
            {
                ref readonly var entityTransform = ref renderEcs.GetTransform(entity);
                ref var finalMatrix = ref renderEcs.GetParentMatrix(entity);

                MatrixMath.CreateModelMatrix(in entityTransform, out var entityMatrix);
                MatrixMath.WriteMultiplyAffine(ref worldMatrix, in entityMatrix, in rootMatrix);

                var particleComp = particleEcs.TryGet(entity);
                if (!particleComp.IsNull)
                {
                    finalMatrix = worldMatrix;
                    particles.GetEmitter(particleComp.Value.Emitter).OriginTranslation = transform.Translation;
                    continue;
                }

                if (animationEcs.Has(entity))
                {
                    finalMatrix = worldMatrix;
                    continue;
                }

                ref readonly var source = ref renderEcs.GetSource(entity);

                var bp = sceneObject.GetModelBlueprint(0);
                var model = assetStore.Get<Model>(bp.ModelId);

                ref readonly var meshMatrix = ref model.WorldTransforms[source.MeshIndex];
                MatrixMath.WriteMultiplyAffine(ref finalMatrix, in meshMatrix, in worldMatrix);
            }
        }

        _store.ClearDirty();
    }
}