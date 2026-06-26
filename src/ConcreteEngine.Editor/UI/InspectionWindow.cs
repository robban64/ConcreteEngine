using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.UI;

internal sealed class InspectionWindow : EditorWindow
{
    public override ReadOnlySpan<byte> Id => WindowRoot.RightWindowId;

    public StateEnums ActiveState { get; private set; }

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
            case StateEnums.None: return;
            case StateEnums.AssetInspector: _assetInspectorPanel.OnDraw(); break;
            case StateEnums.SceneInspector: _sceneInspectorPanel.OnDraw(); break;
            case StateEnums.Camera: _cameraPanel.OnDraw(); break;
            case StateEnums.Lighting: _lightingPanel.OnDraw(); break;
            case StateEnums.Visual: _visualPanel.OnDraw(); break;
            default: Throwers.Unreachable(nameof(ActiveState)); break;
        }
    }

    private void OnStateOnContextChanged(EditorContext prev, EditorContext next)
    {
        SelectionContext prevSelection = prev.Selection, nextSelection = next.Selection;

        if (prevSelection == nextSelection) return;

        ActiveState = StateEnums.None;
        if (prevSelection.SelectedSceneId != nextSelection.SelectedSceneId)
            ActiveState = StateEnums.SceneInspector;
        else if (prevSelection.SelectedAssetId != nextSelection.SelectedAssetId)
            ActiveState = StateEnums.AssetInspector;
        else if (prevSelection.FixedInspector != nextSelection.FixedInspector)
        {
            ActiveState = nextSelection.FixedInspector switch
            {
                FixedInspectorId.None => StateEnums.None,
                FixedInspectorId.Camera => StateEnums.Camera,
                FixedInspectorId.Lighting => StateEnums.Lighting,
                FixedInspectorId.Visual => StateEnums.Visual,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        Enabled = ActiveState != StateEnums.None;
    }
}