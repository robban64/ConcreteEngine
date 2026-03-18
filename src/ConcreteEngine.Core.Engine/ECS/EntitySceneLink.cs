namespace ConcreteEngine.Core.Engine.ECS;


public sealed class EntitySceneLink(RenderEntityCore renderEcs, GameEntityCore gameEcs)
{
    private int[] _renderSceneHandles = new int[renderEcs.Capacity];
    private int[] _gameSceneHandles = new int[gameEcs.Capacity];

    private readonly RenderEntityCore _renderEcs = renderEcs;
    private readonly GameEntityCore _gameEcs = gameEcs;

    //
    public int GetSceneHandleBy(RenderEntityId entity) => _renderSceneHandles[entity.Index()];

    public int GetSceneHandleBy(GameEntityId entity) => _gameSceneHandles[entity.Index()];

    //
    public void BindSceneHandle(RenderEntityId entity, int sceneHandle)
        => _renderSceneHandles[entity.Index()] = sceneHandle;

    public void BindSceneHandle(GameEntityId entity, int sceneHandle)
        => _renderSceneHandles[entity.Index()] = sceneHandle;

    //
    public void UnbindSceneHandle(RenderEntityId entity, int sceneHandle)
        => _renderSceneHandles[entity.Index()] = sceneHandle;

    public void UnbindSceneHandle(GameEntityId entity, int sceneHandle)
        => _renderSceneHandles[entity.Index()] = sceneHandle;
}