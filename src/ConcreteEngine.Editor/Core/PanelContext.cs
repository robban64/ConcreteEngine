using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Controller.Proxy;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelContext(EventManager eventManager, SelectionManager selection)
{
    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);

    public SceneObjectProxy? SceneProxy => selection.SceneProxy;
    public SceneObjectId SelectedSceneId => SceneProxy?.Id ?? SceneObjectId.Empty;

    public AssetObjectProxy? AssetProxy => selection.AssetProxy;
    public AssetId SelectedAssetId => AssetProxy?.Asset.Id ?? AssetId.Empty;
}