using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Render;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine;

internal sealed class SceneProcessor(SceneStore store)
{
    public void Update(float dt)
    {
        if(store.DirtyCount > 0)
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
        foreach (var id in store.GetDirtySpan())
        {
            var sceneObject = store.GetInternal(id);
            var dirtyFlag = sceneObject.Dirty;
            if ((dirtyFlag & SceneObject.DirtyFlags.Visibility) != 0)
                UpdateVisibility(sceneObject);
            if ((dirtyFlag & SceneObject.DirtyFlags.Transform) != 0)
                UpdateTransform(sceneObject);
            if ((dirtyFlag & SceneObject.DirtyFlags.Instance) != 0)
                UpdateInstance(sceneObject);

            sceneObject.ClearDirty();
        }

        store.ClearDirty();
    }

    private void UpdateVisibility(SceneObject sceneObject)
    {
        var renderEcs = Ecs.Render.Core;
        var visibility = sceneObject.Visible;
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            renderEcs.ToggleVisibilityFlag(entity, VisibilityFlags.ForceHidden, visibility);
        }
    }

    private void UpdateInstance(SceneObject sceneObject)
    {
        foreach (var it in sceneObject.GetInstances())
        {
            if (!it.IsDirty) continue;
            it.Commit();
        }
    }

    private void UpdateTransform(SceneObject sceneObject)
    {
        var renderEcs = Ecs.Render.Core;
        var particleEcs = Ecs.GetRenderStore<ParticleComponent>();
        var skinnedEcs = Ecs.GetRenderStore<SkinLinkComponent>();

        MatrixMath.CreateModelMatrix(in sceneObject.Transform.GetTransform(), out var rootMatrix);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            ref readonly var entityTransform = ref Ecs.Render.Core.GetTransform(entity);
            ref var finalMatrix = ref renderEcs.GetMatrix(entity);

            MatrixMath.CreateModelMatrix(in entityTransform, out var worldMatrix);
            MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            //MatrixMath.MultiplyAffine(in entityMatrix, in rootMatrix, out var worldMatrix);

            var particleComp = particleEcs.TryGet(entity);
            if (!particleComp.IsNull)
            {
                finalMatrix = worldMatrix;
                var emitter = ParticleSystem.Instance.GetEmitter(particleComp.Value.Emitter);
                emitter.Translation = sceneObject.Transform.Translation;
                continue;
            }

            if (skinnedEcs.Has(entity))
            {
                finalMatrix = worldMatrix;
                continue;
            }

            var instance = sceneObject.GetInstance<ModelInstance>();
            ref readonly var source = ref renderEcs.GetSource(entity);
            ref readonly var meshMatrix = ref instance.AssetModel.Meshes[source.MeshIndex].WorldTransform;
            MatrixMath.MultiplyAffine(ref finalMatrix, in meshMatrix, in worldMatrix);

            //finalMatrix = meshMatrix * worldMatrix;
        }
    }
}