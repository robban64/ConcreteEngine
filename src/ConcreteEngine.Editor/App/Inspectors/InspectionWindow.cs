using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

//TODO
internal sealed class InspectionWindow : EditorWindow
{
    public override ReadOnlySpan<byte> Id => WindowRoot.RightWindowId;

    public InspectorId ActiveState { get; private set; }

    private readonly CameraPanel _cameraPanel;
    private readonly VisualPanel _visualPanel;
    private readonly LightingPanel _lightingPanel;
    private readonly AssetInspectorPanel _assetInspectorPanel;
    private readonly SceneInspectorPanel _sceneInspectorPanel;

    private readonly EditorPanel[] _panels;

    public InspectionWindow(StateManager state) : base(state)
    {
        var cameraPanel = _cameraPanel = new CameraPanel(state);
        var visualPanel = _visualPanel = new VisualPanel(state);
        var lightingPanel = _lightingPanel = new LightingPanel(state);
        var assetInspectorPanel = _assetInspectorPanel = new AssetInspectorPanel(state);
        var sceneInspectorPanel = _sceneInspectorPanel = new SceneInspectorPanel(state);
        _panels = [assetInspectorPanel, sceneInspectorPanel, cameraPanel, lightingPanel, visualPanel];
    }

    protected override void OnCreate()
    {
        State.ContextChanged += OnStateOnContextChanged;

        foreach (var panel in _panels)
            panel.OnCreate();

        foreach (var panel in _panels)
        {
            panel.OnAttach();
        }
    }

    public override void OnUpdateDiagnostic()
    {
        var index = (int)ActiveState - 1;
        if ((uint)index >= (uint)_panels.Length) return;
        _panels[index].OnUpdateDiagnostic();
    }

    protected override void OnDraw()
    {
        var activeState = (int)ActiveState - 1;
        if (activeState < 0) activeState = 2;
        _panels[activeState].OnDraw();
    }

    private void OnStateOnContextChanged(EditorContext prev, EditorContext next)
    {
        SelectionContext prevSelection = prev.Selection, nextSelection = next.Selection;

        ActiveState = InspectorId.None;
        
        if(nextSelection.HasNewAsset(prevSelection)) ActiveState = InspectorId.Asset;
        else if(nextSelection.HasNewScene(prevSelection)) ActiveState = InspectorId.SceneObject;
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