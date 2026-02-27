using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Editor.Core;

internal sealed class PanelContext(EventManager eventManager, SelectionManager selection, GfxResourceApi gfxApi)
{
    public readonly SelectionManager Selection = selection;

    public SceneObjectProxy? SceneProxy => Selection.SceneProxy;
    public SceneObjectId SelectedSceneId => SceneProxy?.Id ?? SceneObjectId.Empty;

    public InspectAsset? SelectedAsset => Selection.SelectedAsset;
    public AssetId SelectedAssetId => Selection.SelectedAssetId;

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);


}