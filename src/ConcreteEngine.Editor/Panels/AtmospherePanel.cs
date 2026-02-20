using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Panels.Fields;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class AtmospherePanel(PanelContext context)
    : EditorPanel(PanelId.Atmosphere, context)
{
    public override void Enter()
    {
        ShadowPanelFields.Distance.Refresh();
        ShadowPanelFields.Strength.Refresh();
        ShadowPanelFields.ConstBias.Refresh();
        ShadowPanelFields.PcfRadius.Refresh();
        ShadowPanelFields.SlopeBias.Refresh();
        ShadowPanelFields.ZPad.Refresh();

        FogPanelFields.Strength.Refresh();
        FogPanelFields.BaseHeight.Refresh();
        FogPanelFields.Density.Refresh();
        FogPanelFields.Falloff.Refresh();
        FogPanelFields.FogColor.Refresh();
        FogPanelFields.Influence.Refresh();
        FogPanelFields.MaxDistance.Refresh();
        FogPanelFields.Scattering.Refresh();

    }

    public override void Draw(in FrameContext ctx)
    {
        ImGui.BeginChild("atmosphere"u8, ImGuiChildFlags.AlwaysUseWindowPadding);
        
        ImGui.SeparatorText("Atmosphere"u8);

        if (ImGui.BeginTabBar("##tabs"u8))
        {
            if (ImGui.BeginTabItem("Fog"u8))
            {
                DrawFog();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.EndChild();
    }

    
    private static void DrawFog()
    {
        var width = ImGui.GetContentRegionAvail().X;

        ImGui.SeparatorText("Fog Properties"u8);

        FogPanelFields.FogColor.DrawField(true, width);
        FogPanelFields.Density.DrawField(false);

        ImGui.SeparatorText("Fog Height"u8);
        FogPanelFields.BaseHeight.DrawField(false);
        FogPanelFields.Falloff.DrawField(false);
        FogPanelFields.Influence.DrawField(false);

        ImGui.SeparatorText("Fog Optics"u8);
        FogPanelFields.Scattering.DrawField(false);
        FogPanelFields.MaxDistance.DrawField(false);
        FogPanelFields.Strength.DrawField(false);


    }

}
