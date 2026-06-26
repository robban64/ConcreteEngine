using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.UI;

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

    public override void OnCreate()
    {
        State.ContextChanged += OnStateOnContextChanged;

        var arena = TextBuffers.PersistentArena;
        var allocator = arena.MakeBuilder();

        foreach (var panel in _panels)
            panel.OnCreate(allocator);

        var memory = arena.CommitBuilder(allocator);
        foreach (var panel in _panels)
        {
            panel.Memory = memory;
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
        switch (ActiveState)
        {
            case InspectorId.None: return;
            case InspectorId.Asset: _assetInspectorPanel.OnDraw(); break;
            case InspectorId.SceneObject: _sceneInspectorPanel.OnDraw(); break;
            case InspectorId.Camera: _cameraPanel.OnDraw(); break;
            case InspectorId.Lighting: _lightingPanel.OnDraw(); break;
            case InspectorId.Visual: _visualPanel.OnDraw(); break;
            default: Throwers.Unreachable(nameof(ActiveState)); break;
        }
    }

    private void OnStateOnContextChanged(EditorContext prev, EditorContext next)
    {
        SelectionContext prevSelection = prev.Selection, nextSelection = next.Selection;

        if (prevSelection == nextSelection) return;

        ActiveState = InspectorId.None;
        if (prevSelection.SelectedSceneId != nextSelection.SelectedSceneId)
            ActiveState = InspectorId.SceneObject;
        else if (prevSelection.SelectedAssetId != nextSelection.SelectedAssetId)
            ActiveState = InspectorId.Asset;
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

        Enabled = ActiveState != InspectorId.None;
    }
}