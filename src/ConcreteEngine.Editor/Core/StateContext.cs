using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateContext(
    EventManager eventManager,
    SelectionManager selection,
    PanelState panelState,
    GfxResourceApi gfxApi)
{
    public readonly PanelState Panels = panelState;
    public readonly SelectionManager Selection = selection;

    public SceneObjectInspector? SelectedSceneObject => Selection.SelectedSceneObject;
    public SceneObjectId SelectedSceneId => SelectedSceneObject?.Id ?? SceneObjectId.Empty;

    public InspectAsset? SelectedAsset => Selection.SelectedAsset;
    public AssetId SelectedAssetId => Selection.SelectedAssetId;

    public bool IsMetricMode => Panels.RightPanelId == PanelId.MetricsRight;

    public void EmitTransition(TransitionMessage msg) => Panels.EmitTransition(msg);

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);

    public ImTextureRefPtr GetTextureRefPtr(TextureId id)
    {
        return ImGui.ImTextureRef(new ImTextureID(gfxApi.GetNativeHandle(id)));
    }
}