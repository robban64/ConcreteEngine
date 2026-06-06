// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

using ConcreteEngine.Core.Engine.Scene;

namespace ConcreteEngine.Core.Engine.ECS.Integration;

public sealed class EntitySceneLink
{
    private SceneObjectId[] _renderToSceneId;
    private SceneObjectId[] _gameToSceneId;


    public EntitySceneLink(RenderEntityCore renderEcs, GameEntityCore gameEcs)
    {
        _renderToSceneId = new SceneObjectId[renderEcs.Capacity];
        _gameToSceneId = new SceneObjectId[gameEcs.Capacity];

        renderEcs.AddResizeCallback(RenderResizeCallback);
        gameEcs.AddResizeCallback(GameResizeCallback);
    }

    public void RenderResizeCallback(EcsStore store) => Array.Resize(ref _renderToSceneId, store.Capacity);
    public void GameResizeCallback(EcsStore store) => Array.Resize(ref _gameToSceneId, store.Capacity);

    //
    public SceneObjectId GetSceneHandleBy(RenderEntityId entity) => _renderToSceneId[entity.Index()];
    public SceneObjectId GetSceneHandleBy(GameEntityId entity) => _gameToSceneId[entity.Index()];

    //
    public void BindSceneHandle(RenderEntityId entity, SceneObjectId sceneId) =>
        _renderToSceneId[entity.Index()] = sceneId;

    public void BindSceneHandle(GameEntityId entity, SceneObjectId sceneId) => _gameToSceneId[entity.Index()] = sceneId;

    //
    public void UnbindSceneHandle(RenderEntityId entity) => _renderToSceneId[entity.Index()] = SceneObjectId.Empty;
    public void UnbindSceneHandle(GameEntityId entity) => _gameToSceneId[entity.Index()] = SceneObjectId.Empty;
}