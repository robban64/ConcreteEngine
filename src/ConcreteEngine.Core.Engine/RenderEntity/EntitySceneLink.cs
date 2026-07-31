// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed class EntitySceneLink
{
    private SceneObjectId[] _renderToSceneId;

    internal void Resize(int newSize)
    {
        if(newSize <= _renderToSceneId.Length) return;
        Array.Resize(ref _renderToSceneId, newSize);
    }

    public EntitySceneLink(RenderEntityCore renderEcs)
    {
        _renderToSceneId = new SceneObjectId[renderEcs.Capacity];
        renderEcs.AddResizeCallback(RenderResizeCallback);
    }

    private void RenderResizeCallback(int capacity) => Array.Resize(ref _renderToSceneId, capacity);
    //
    public SceneObjectId GetSceneHandleBy(RenderEntityId entity) => _renderToSceneId[entity.Index()];

    //
    public void BindSceneHandle(RenderEntityId entity, SceneObjectId sceneId) =>
        _renderToSceneId[entity.Index()] = sceneId;

    //
    public void UnbindSceneHandle(RenderEntityId entity) => _renderToSceneId[entity.Index()] = SceneObjectId.Empty;
}