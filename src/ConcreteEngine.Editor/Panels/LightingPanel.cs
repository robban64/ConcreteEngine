using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Panels.Fields;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;


internal sealed class LightingPanel(PanelContext context) : EditorPanel(PanelId.Lighting, context)
{
    public override void Enter()
    {
        LightPanelFields.Direction.Refresh();
        LightPanelFields.Diffuse.Refresh();
        LightPanelFields.Intensity.Refresh();
        LightPanelFields.Specular.Refresh();
        LightPanelFields.Ambient.Refresh();
        LightPanelFields.AmbientGround.Refresh();
        LightPanelFields.Exposure.Refresh();
        
        ShadowPanelFields.Distance.Refresh();
        ShadowPanelFields.Strength.Refresh();
        ShadowPanelFields.ConstBias.Refresh();
        ShadowPanelFields.PcfRadius.Refresh();
        ShadowPanelFields.SlopeBias.Refresh();
        ShadowPanelFields.ZPad.Refresh();
    }

    public override void Draw(in FrameContext ctx)
    {
        ImGui.BeginChild("light"u8, ImGuiChildFlags.AlwaysUseWindowPadding);
        
        ImGui.SeparatorText("Illumination"u8);

        if (ImGui.BeginTabBar("##tabs"u8))
        {
            if (ImGui.BeginTabItem("Light"u8))
            {
                DrawLight();
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Shadow"u8))
            {
                DrawShadow();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.EndChild();
    }

    
    private static void DrawLight()
    {
        var width = ImGui.GetContentRegionAvail().X;
        
        ImGui.SeparatorText("Directional Light"u8);
        LightPanelFields.Direction.DrawField(true, width);
        LightPanelFields.Diffuse.DrawField(true, width);
        LightPanelFields.Intensity.DrawField(false);
        LightPanelFields.Specular.DrawField(false);

        ImGui.Spacing();
        ImGui.SeparatorText("Ambient Light"u8);
        LightPanelFields.Ambient.DrawField(true, width);
        LightPanelFields.AmbientGround.DrawField(true, width);
        LightPanelFields.Exposure.DrawField(false);

    }

    
    private static void DrawShadow()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Shadow Map Size"u8);

        ShadowPanelFields.ShadowSizeCombo.DrawField(true, width);

        ImGui.SeparatorText("Shadow Properties"u8);

        ShadowPanelFields.Distance.DrawField(false);
        ShadowPanelFields.Strength.DrawField(false);
        
        ImGui.Spacing();
        ImGui.Separator();
        ShadowPanelFields.ConstBias.DrawField(false);
        ShadowPanelFields.PcfRadius.DrawField(false);
        ShadowPanelFields.SlopeBias.DrawField(false);
        ShadowPanelFields.ZPad.DrawField(false);

    }

    
}