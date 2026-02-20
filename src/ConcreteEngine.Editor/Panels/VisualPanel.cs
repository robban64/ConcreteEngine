using System.Numerics;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Definitions;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.Fields;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.InputComponents;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel(PanelContext context) : EditorPanel(PanelId.Visual, context)
{
    public override void Draw(in FrameContext ctx)
    {
        ImGui.BeginChild("visual"u8, ImGuiChildFlags.AlwaysUseWindowPadding);
        
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        PostEffectPanelFields.GradeExposure.DrawField(false);
        PostEffectPanelFields.GradeSaturation.DrawField(false);
        PostEffectPanelFields.GradeContrast.DrawField(false);
        PostEffectPanelFields.GradeWarmth.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        PostEffectPanelFields.WbTint.DrawField(false);
        PostEffectPanelFields.WbStrength.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        PostEffectPanelFields.BloomIntensity.DrawField(false);
        PostEffectPanelFields.BloomThreshold.DrawField(false);
        PostEffectPanelFields.BloomRadius.DrawField(false);
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        PostEffectPanelFields.FxVignette.DrawField(false);
        PostEffectPanelFields.FxGrain.DrawField(false);
        PostEffectPanelFields.FxSharpen.DrawField(false);
        PostEffectPanelFields.FxRolloff.DrawField(false);
        ImGui.EndGroup();

        ImGui.EndChild();
    }
}