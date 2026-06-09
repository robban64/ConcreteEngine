using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Engine.Render;
using Ecs = ConcreteEngine.Core.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine;

internal sealed class SceneProcessor(SceneManager sceneManager)
{
    private readonly SceneStore _store = sceneManager.Store;
    
    public void Update(float dt)
    {
        if(sceneManager.DirtyCount > 0)
            CommitSceneObjects();
        
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

    private void CommitSceneObjects()
    {
        foreach (var id in sceneManager.GetDirtySpan())
        {
            var sceneObject = _store.GetInternal(id);
            
            var dirtyFlag = sceneObject.Dirty;
            
            if ((dirtyFlag & SceneObject.DirtyFlags.Visibility) != 0)
                UpdateVisibility(sceneObject);
            if ((dirtyFlag & SceneObject.DirtyFlags.Instance) != 0)
                UpdateInstance(sceneObject);
            if ((dirtyFlag & SceneObject.DirtyFlags.Transform) != 0)
            {
                UpdateTransform(sceneObject);
                UpdateBounds(sceneObject);
            }            

            sceneObject.ClearDirty();
        }

        sceneManager.ClearDirty();
    }

    private void UpdateVisibility(SceneObject sceneObject)
    {
        var renderEcs = Ecs.Render.Core;
        var visibility = sceneObject.Visible;
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            renderEcs.ToggleVisibility(entity, VisibilityFlags.ForceHidden, visibility);
        }
    }

    private void UpdateInstance(SceneObject sceneObject)
    {
        foreach (var it in sceneObject.GetInstances())
        {
            if (!it.IsDirty) continue;
            it.Commit(sceneObject);
        }
    }

    private void UpdateTransform(SceneObject sceneObject)
    {
        var particleEcs = Ecs.GetRenderStore<ParticleComponent>();
        var skinnedEcs = Ecs.GetRenderStore<SkinLinkComponent>();

        MatrixMath.CreateModelMatrix(in sceneObject.Transform.GetTransform(), out var rootMatrix);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            ref readonly var entityTransform = ref Ecs.Render.Core.GetLocalTransform(entity);
            MatrixMath.CreateModelMatrix(in entityTransform, out var worldMatrix);
            MatrixMath.MultiplyAffine(ref worldMatrix, in rootMatrix);

            ref var finalMatrix = ref Ecs.Render.Core.GetWorldMatrix(entity);

            var particleComp = particleEcs.TryGet(entity);
            if (!particleComp.IsNull)
            {
                finalMatrix = worldMatrix;
                var emitter = ParticleManager.Instance.Get(particleComp.Value.Emitter);
                emitter.Translation = sceneObject.Transform.Translation;
                continue;
            }

            if (skinnedEcs.Has(entity))
            {
                finalMatrix = worldMatrix;
                continue;
            }

            var instance = sceneObject.GetInstance<ModelInstance>();
            var meshIndex = Ecs.Render.Core.GetSource(entity).MeshIndex;
            ref readonly var meshMatrix = ref instance.AssetModel.Meshes[meshIndex].WorldTransform;
            MatrixMath.MultiplyAffine(ref finalMatrix, in meshMatrix, in worldMatrix);
        }
    }

    private void UpdateBounds(SceneObject sceneObject)
    {
        if(!sceneObject.TryGetInstance<ModelInstance>(out var instance)) return;
        var isAnimated = instance.AssetModel.Animation != null;

        foreach (var entity in instance.GetRenderEntities())
        {
            ref readonly var matrix = ref Ecs.Render.Core.GetWorldMatrix(entity);

            var meshIndex = Ecs.Render.Core.GetSource(entity).MeshIndex;
            var local = isAnimated ? instance.LocalBounds : instance.AssetModel.Meshes[meshIndex].LocalBounds;

            ref var bounds = ref Ecs.Render.Core.GetWorldBounds(entity);
            BoundingBox.GetWorldBounds(in local, in matrix, out bounds);
        }

    }
}