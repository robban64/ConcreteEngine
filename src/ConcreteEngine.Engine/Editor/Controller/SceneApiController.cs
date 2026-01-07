using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class SceneApiController(ApiContext context) : IEngineSceneController
{
    private readonly SceneManager _sceneManager = context.SceneManager;
    private readonly SceneStore _sceneStore = context.SceneManager.Store;

    public ReadOnlySpan<ISceneObject> GetSceneObjectSpan() => _sceneManager.Store.GetSceneObjectSpan();
    public ISceneObject GetSceneObject(SceneObjectId id) => _sceneManager.Store.Get(id);
    
    public void Select(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            Ecs.Render.Stores<SelectionComponent>.Store.Add(entity, new SelectionComponent());
        }
    }

    public void Deselect(SceneObjectId id)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        foreach (var entity in sceneObject.GetRenderEntities())
        {
            Ecs.Render.Stores<SelectionComponent>.Store.Remove(entity);
        }
    }

    public void FetchTransform(SceneObjectId id, ref TransformStable transform)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        TransformStable.MakeFrom(in sceneObject.Transform, out transform);
    }

    public void CommitTransform(SceneObjectId id, in TransformStable transform)
    {
        var sceneObject = _sceneManager.Store.Get(id);
        sceneObject.Transform.Translation = transform.Translation;
        sceneObject.Transform.Rotation = transform.Rotation;
        sceneObject.Transform.Scale = transform.Scale;
    }
}