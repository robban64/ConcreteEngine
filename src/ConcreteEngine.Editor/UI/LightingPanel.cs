using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Lib.Impl;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class LightingPanel : EditorPanel
{
    private readonly InspectLightningFields _inspectFields = InspectorFieldProvider.Instance.LightningFields;

    public override void OnEnter() => _inspectFields.Refresh();


    public LightingPanel(StateContext context) : base(PanelId.Lighting, context)
    {
        _inspectFields.ShadowSizeCombo.Layout = FieldLayout.None;
    }

    public override void OnDraw(FrameContext ctx)
    {
        ImGui.SeparatorText("Illumination"u8);

        if (!ImGui.BeginTabBar("##tabs"u8)) return;

        if (ImGui.BeginTabItem("Light"u8))
        {
            _inspectFields.Draw(0, 2);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Shadow"u8))
        {
            _inspectFields.Draw(2, 5);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Fog"u8))
        {
            _inspectFields.Draw(5);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
/*

    private static void DrawLight()
    {
        ImGui.SeparatorText("Directional Light"u8);
        LightFields.Direction.Draw();
        LightFields.Diffuse.Draw();
        LightFields.Intensity.Draw();
        LightFields.Specular.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("Ambient Light"u8);
        LightFields.Ambient.Draw();
        LightFields.AmbientGround.Draw();
        LightFields.Exposure.Draw();
    }


    private static void DrawShadow()
    {
        ImGui.SeparatorText("Shadow Map Size"u8);

        ShadowFields.ShadowSizeCombo.Draw();

        ImGui.SeparatorText("Shadow Projection"u8);
        ShadowFields.ShadowProjectionFields.Draw();

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.SeparatorText("Shadow Visuals"u8);
        ShadowFields.ShadowVisualFields.Draw();
    }
    */
}