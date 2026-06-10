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
            
            if ((dirtyFlag & SceneDirtyFlags.Visibility) != 0)
                UpdateVisibility(sceneObject);
            if ((dirtyFlag & SceneDirtyFlags.Instance) != 0)
                CommitInstances(sceneObject);
            if ((dirtyFlag & SceneDirtyFlags.Transform) != 0)
                UpdateTransform(sceneObject);

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

    private void CommitInstances(SceneObject sceneObject)
    {
        foreach (var it in sceneObject.GetInstances())
        {
            if (!it.IsDirty) continue;
            it.Commit();
        }
    }

    private void UpdateTransform(SceneObject sceneObject)
    {
        foreach (var instance in sceneObject.GetInstances())
            instance.ApplyTransform();
    }
    
}