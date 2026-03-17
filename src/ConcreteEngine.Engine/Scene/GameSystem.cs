using System.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

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
        var particles = world.Particles;

        var renderEcs = _renderEcs;
        var particleEcs = Ecs.Render.Stores<ParticleComponent>.Store;
        var animationEcs = Ecs.Render.Stores<RenderAnimationComponent>.Store;

        var worldMatrix = Matrix4x4.Identity;
        foreach (var id in _store.DirtyIds)
        {
            var sceneObject = _store.Get(new SceneObjectId(id, 0));

            ref readonly var transform = ref sceneObject.GetTransform();

            MatrixMath.CreateModelMatrix(in transform, out var rootMatrix);
            foreach (var entity in sceneObject.GetRenderEntities())
            {
                ref readonly var entityTransform = ref renderEcs.GetTransform(entity);
                ref var finalMatrix = ref renderEcs.GetParentMatrix(entity);

                MatrixMath.CreateModelMatrix(in entityTransform, out var entityMatrix);
                MatrixMath.MultiplyAffine(in entityMatrix, in rootMatrix, out worldMatrix);

                var particleComp = particleEcs.TryGet(entity);
                if (!particleComp.IsNull)
                {
                    finalMatrix = worldMatrix;
                    particles.GetEmitter(particleComp.Value.Emitter).GetState().Translation = transform.Translation;
                    continue;
                }

                if (animationEcs.Has(entity))
                {
                    finalMatrix = worldMatrix;
                    continue;
                }

                ref readonly var source = ref renderEcs.GetSource(entity);

                var instance = sceneObject.GetInstance<ModelInstance>();

                ref readonly var meshMatrix = ref instance.Asset.Meshes[source.MeshIndex].WorldTransform;
                MatrixMath.WriteMultiplyAffine(ref finalMatrix, in meshMatrix, in worldMatrix);
            }
        }

        _store.ClearDirty();
    }
}