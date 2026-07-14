using ConcreteEngine.Core.Common;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

//TODO
internal sealed class InspectionWindow : EditorWindow
{
    public override ReadOnlySpan<byte> Id => WindowRoot.RightWindowId;

    public InspectorId ActiveState { get; private set; }

    private readonly CameraInspector _cameraInspector;
    private readonly PostEffectSettingsInspector _postFxInspector;
    
    private readonly LightingPanel _lightingPanel;
    private readonly AssetInspectorPanel _assetInspectorPanel;
    private readonly SceneInspectorPanel _sceneInspectorPanel;

    public InspectionWindow(StateManager state) : base(state)
    {
        _cameraInspector = new CameraInspector();
        _postFxInspector = new PostEffectSettingsInspector();
        _lightingPanel = new LightingPanel();
        _assetInspectorPanel = new AssetInspectorPanel(state);
        _sceneInspectorPanel = new SceneInspectorPanel(state);
    }

    protected override void OnCreate()
    {
        State.ContextChanged += OnStateOnContextChanged;
    }

    protected override void OnDraw()
    {
        switch (ActiveState)
        {
            case InspectorId.None: break;
            case InspectorId.Camera: _cameraInspector.Draw(); break;
            case InspectorId.Visual: _postFxInspector.Draw(); break;
            case InspectorId.Asset: _assetInspectorPanel.Draw(); break;
            case InspectorId.SceneObject: _sceneInspectorPanel.Draw(); break;
            case InspectorId.Lighting: _lightingPanel.Draw(); break;
            default: Throwers.Unreachable(nameof(ActiveState)); return;
        }
    }

    private void OnStateOnContextChanged(EditorContext prev, EditorContext next)
    {
        SelectionContext prevSelection = prev.Selection, nextSelection = next.Selection;

        ActiveState = InspectorId.Camera;

        if (nextSelection.HasNewAsset(prevSelection)) ActiveState = InspectorId.Asset;
        else if (nextSelection.HasNewScene(prevSelection)) ActiveState = InspectorId.SceneObject;
        else if (prevSelection.FixedInspector != nextSelection.FixedInspector)
        {
            ActiveState = nextSelection.FixedInspector switch
            {
                FixedInspectorId.None => InspectorId.None,
                FixedInspectorId.Camera => InspectorId.Camera,
                FixedInspectorId.Lighting => InspectorId.Lighting,
                FixedInspectorId.Visual => InspectorId.Visual,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        if (ActiveState != InspectorId.None) return;

        if (nextSelection.SelectedAssetId == default && ActiveState == InspectorId.Asset ||
            nextSelection.SelectedSceneId == default && ActiveState == InspectorId.SceneObject)
        {
            ActiveState = InspectorId.Camera;
        }

        //Enabled = ActiveState != InspectorId.None;
    }
}