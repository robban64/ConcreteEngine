using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App;

internal sealed class InspectionWindow : EditorWindow
{
    public override ReadOnlySpan<byte> Id => WindowRoot.RightWindowId;

    public InspectorId ActiveState { get; private set; }

    private Inspector _inspector = null!;

    public InspectionWindow(StateManager state) : base(state)
    {
        _ = new CameraInspector();
        _ = new PostEffectSettingsInspector();
        _ = new LightingSettingsInspector();
        _ = new EnvironmentSettingsInspector();

        _ = new AssetObjectInspector(state);
        _ = new SceneObjectInspector(state);
    }

    protected override void OnCreate() => State.ContextChanged += OnStateOnContextChanged;

    protected override void OnDraw()
    {
        if (ActiveState == InspectorId.None) return;
        _inspector.Draw();
    }

    private void OnStateOnContextChanged(EditorContext prev, EditorContext next)
    {
        SelectionContext prevSelection = prev.Selection, nextSelection = next.Selection;

        var activeState = InspectorId.Camera;

        if (nextSelection.HasNewAsset(prevSelection)) activeState = InspectorId.Asset;
        else if (nextSelection.HasNewScene(prevSelection)) activeState = InspectorId.SceneObject;
        else if (prevSelection.FixedInspector != nextSelection.FixedInspector)
        {
            activeState = nextSelection.FixedInspector switch
            {
                FixedInspectorId.None => InspectorId.None,
                FixedInspectorId.Camera => InspectorId.Camera,
                FixedInspectorId.Lighting => InspectorId.Lighting,
                FixedInspectorId.Environment => InspectorId.Environment,
                FixedInspectorId.PostFx => InspectorId.PostFx,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        if (nextSelection.SelectedAssetId == default && activeState == InspectorId.Asset ||
            nextSelection.SelectedSceneId == default && activeState == InspectorId.SceneObject)
        {
            activeState = InspectorId.Camera;
        }

        switch (activeState)
        {
            case InspectorId.None: break;
            case InspectorId.Asset: _inspector = AssetObjectInspector.Instance; break;
            case InspectorId.SceneObject: _inspector = SceneObjectInspector.Instance; break;

            case InspectorId.Camera: _inspector = CameraInspector.Instance; break;
            case InspectorId.Lighting: _inspector = LightingSettingsInspector.Instance; break;
            case InspectorId.Environment: _inspector = EnvironmentSettingsInspector.Instance; break;
            case InspectorId.PostFx: _inspector = PostEffectSettingsInspector.Instance; break;
            default: throw new ArgumentOutOfRangeException();
        }

        ActiveState = activeState;
    }
}