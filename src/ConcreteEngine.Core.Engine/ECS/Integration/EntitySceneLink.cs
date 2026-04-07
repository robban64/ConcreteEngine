// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace ConcreteEngine.Core.Engine.ECS.Integration;

public sealed class EntitySceneLink
{
    private int[] _renderToSceneId;
    private int[] _gameToSceneId;

    private readonly RenderEntityCore _renderEcs;
    private readonly GameEntityCore _gameEcs;

    public EntitySceneLink(RenderEntityCore renderEcs, GameEntityCore gameEcs)
    {
        _renderToSceneId = new int[renderEcs.Capacity];
        _gameToSceneId = new int[gameEcs.Capacity];
        _renderEcs = renderEcs;
        _gameEcs = gameEcs;

        _renderEcs.AddResizeCallback(RenderResizeCallback);
        _gameEcs.AddResizeCallback(GameResizeCallback);
    }

    public void RenderResizeCallback(EcsStore store) => Array.Resize(ref _renderToSceneId, store.Capacity);
    public void GameResizeCallback(EcsStore store) => Array.Resize(ref _gameToSceneId, store.Capacity);

    //
    public int GetSceneHandleBy(RenderEntityId entity) => _renderToSceneId[entity.Index()];
    public int GetSceneHandleBy(GameEntityId entity) => _gameToSceneId[entity.Index()];

    //
    public void BindSceneHandle(RenderEntityId entity, int sceneId) => _renderToSceneId[entity.Index()] = sceneId;

    public void BindSceneHandle(GameEntityId entity, int sceneId) => _gameToSceneId[entity.Index()] = sceneId;

    //
    public void UnbindSceneHandle(RenderEntityId entity, int sceneId) => _renderToSceneId[entity.Index()] = 0;
    public void UnbindSceneHandle(GameEntityId entity, int sceneId) => _gameToSceneId[entity.Index()] = 0;
}