using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed unsafe class InspectionWindow : EditorWindow
{
    public override ReadOnlySpan<byte> Id => WindowRoot.RightWindowId;

    public InspectorId ActiveState { get; private set; }

    private Action _inspectionDrawer;
    private AvgFrameTimer avg;

    public InspectionWindow(StateManager state) : base(state)
    {
        _ = new CameraInspector();
        
        _ = new PostEffectSettingsInspector();
        _ = new DirectionalLightInspector();
        _ = new ShadowSettingsInspector();
        _ = new EnvironmentSettingsInspector();
        _ = new AssetInspectorPanel(state);
        _ = new SceneInspectorPanel(state);

        _inspectionDrawer = static () => CameraInspector.Instance.Draw();
    }

    protected override void OnCreate() => State.ContextChanged += OnStateOnContextChanged;

    protected override void OnDraw()
    {
        if (ActiveState == InspectorId.None) return;

        avg.BeginSample();
        _inspectionDrawer();
        if (avg.EndSample() > 40) avg.ResetAndPrint();
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
                FixedInspectorId.Visual => InspectorId.Visual,
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
            case InspectorId.Camera: _inspectionDrawer = static () => CameraInspector.Instance.Draw(); break;
            case InspectorId.Visual: _inspectionDrawer = static () => PostEffectSettingsInspector.Instance.Draw(); break;
            case InspectorId.Lighting: _inspectionDrawer = static () => DrawLightning(); break;
            case InspectorId.Asset: _inspectionDrawer = static () => AssetInspectorPanel.Instance.Draw(); break;
            case InspectorId.SceneObject: _inspectionDrawer = static () => SceneInspectorPanel.Instance.Draw(); break;
            default: throw new ArgumentOutOfRangeException();
        }

        ActiveState = activeState;
    }


    private static void DrawLightning()
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Sun"u8))
        {
            DirectionalLightInspector.Instance.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            ShadowSettingsInspector.Instance.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Environment"u8))
        {
            EnvironmentSettingsInspector.Instance.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}