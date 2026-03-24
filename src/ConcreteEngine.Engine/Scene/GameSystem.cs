using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Render;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Scene;

internal sealed class GameSystem(AssetStore assetStore, SceneManager sceneManager, EngineRenderSystem renderSystem)
{
    private readonly SceneManager _sceneManager = sceneManager;
    private readonly SceneStore _store = sceneManager.Store;

    private readonly RenderEntityCore _renderEcs = Ecs.Render.Core;
    private readonly GameEntityCore _gameEcs = Ecs.Game.Core;

    private readonly ParticleManager _particleManager = renderSystem.Particles;

    public void UpdateSimulate(float dt)
    {
        _particleManager.UpdateSimulate(dt);
    }

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

    private void CheckDirty()
    {
        var store = _store;
        foreach (var id in store.DirtyIds)
        {
            var sceneObject = store.Get(new SceneObjectId(id, 0));
            var dirtyFlag = sceneObject.Dirty;
            if ((dirtyFlag & SceneObject.DirtyFlags.Transform) != 0)
                UpdateTransform(sceneObject);
            if ((dirtyFlag & SceneObject.DirtyFlags.Instance) != 0)
                UpdateInstance(sceneObject);

            sceneObject.ClearDirty();
        }

        store.ClearDirty();
    }

    private void UpdateTransform(SceneObject sceneObject)
    {
        var particles = _particleManager;
        var renderEcs = _renderEcs;
        var particleEcs = Ecs.Render.Stores<ParticleComponent>.Store;
        var animationEcs = Ecs.Render.Stores<RenderAnimationComponent>.Store;

        ref readonly var transform = ref sceneObject.GetTransform();

        MatrixMath.CreateModelMatrix(in transform, out var rootMatrix);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            ref readonly var entityTransform = ref renderEcs.GetTransform(entity);
            ref var finalMatrix = ref renderEcs.GetParentMatrix(entity);

            MatrixMath.CreateModelMatrix(in entityTransform, out var entityMatrix);
            MatrixMath.MultiplyAffine(in entityMatrix, in rootMatrix, out var worldMatrix);

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

    private void UpdateInstance(SceneObject sceneObject)
    {
        foreach (var it in sceneObject.GetInstances())
        {
            if (!it.IsDirty) continue;
            it.OnUpdate();
        }
    }
}