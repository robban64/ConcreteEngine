using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.UI.InputComponents;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel() : EditorPanel(PanelId.Visual)
{
    private int _editedField = -1;

    private VisualSelection _kind;

    public readonly SlotState<EditorVisualState> State = new();

    private readonly EnumTabBar<VisualSelection> _tabBar = new(0);

    private readonly SelectionCombo<int> _shadowSizeCombo =
        new(["1024px", "2048px", "4096px", "8192px"], [1024, 2048, 4096, 8192]);

    public override void Update()
    {
        EngineController.WorldController.FetchVisualParams(State.GetView());
    }

    private void OnUpdateShadowSize(SlotState<EditorVisualState> state, int size)
    {
        var existingSize = state.Data.Shadow.ShadowMapSize;
        if (size == existingSize) return;
        Context.EnqueueEvent(new GraphicsSettingsEvent() { ShadowSize = size });
    }

    private void OnSelectionChange(VisualSelection kind)
    {
        _kind = kind;
        _shadowSizeCombo.Sync(State.Data.Shadow.ShadowMapSize);
    }

    public override void Draw(ref FrameContext ctx)
    {
        _editedField = -1;

        if (_tabBar.Draw(out var value))
            OnSelectionChange(value);

        if (ImGui.BeginChild("##visual"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            switch (_kind)
            {
                case VisualSelection.Light: DrawLightState(); break;
                case VisualSelection.Fog: DrawFogState(); break;
                case VisualSelection.Post: DrawPostEffects(); break;
                case VisualSelection.Shadow: DrawShadow(ref ctx); break;
                default: throw new ArgumentOutOfRangeException();
            }

            ImGui.EndChild();
        }

        if (_editedField >= 0)
        {
            Context.EnqueueEvent(new VisualDataEvent(State));
            _editedField = -1;
        }
    }


    private void DrawShadow(ref FrameContext ctx)
    {
        ImGui.PushID("shadow"u8);
        ref var shadow = ref State.Data.Shadow;

        {
            int size = shadow.ShadowMapSize;
            ImGui.BeginGroup();
            ImGui.SeparatorText("Shadow Map Size"u8);
            ImGui.TextUnformatted(ctx.Sw.Write(size));

            if (_shadowSizeCombo.Draw(out var newSize))
                OnUpdateShadowSize(State, newSize);

            ImGui.EndGroup();
            ImGui.Dummy(new Vector2(0, 2));
        }

        var fields = new FormFieldInputs(0, false);

        ImGui.BeginGroup();
        ImGui.SeparatorText("Shadow Setting"u8);

        fields.SliderFloat("Distance"u8, Float1, ref shadow.Distance, 10f, 200f, "%.2f");
        fields.SliderFloat("ZPad"u8, Float1, ref shadow.ZPad, 1f, 200f, "%.2f");
        fields.SliderFloat("ConstBias"u8, Float1, ref shadow.ConstBias, 0.0001f, 0.001f, "%.5f");
        fields.SliderFloat("SlopeBias"u8, Float1, ref shadow.SlopeBias, 0.001f, 0.01f, "%.4f");
        fields.SliderFloat("Strength"u8, Float1, ref shadow.Strength, 0f, 1f, "%.2f");
        fields.SliderFloat("PcfRadius"u8, Float1, ref shadow.PcfRadius, 0.5f, 4f, "%.2f");

        ImGui.EndGroup();
        ImGui.PopID();

        if (fields.HasEdited(out var field)) _editedField = field;
    }

    private void DrawLightState()
    {
        ImGui.PushID("light"u8);

        ref var light = ref State.Data.SunLight;
        ref var ambient = ref State.Data.Ambient;

        var fields = FormFieldInputs.MakeVertical();

        ImGui.SeparatorText("Directional Light"u8);

        fields.DragFloat("Direction"u8, Float3, ref light.Direction.X, 0.01f, -1f, 1f, "%.2f");
        fields.ColorEdit3("Diffuse"u8, ref light.Diffuse.X);
        fields.ToggleDefault();
        fields.DragFloat("Intensity"u8, Float1, ref light.Intensity, 0.01f, 0.0f, 10.0f, "%.3f");
        fields.DragFloat("Specular"u8, Float1, ref light.Specular, 0.01f, 0.0f, 10.0f, "%.3f");
        fields.TopLabel = true;

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Ambient Light"u8);

        fields.ToggleVertical();
        fields.ColorEdit3("Ambient"u8, ref ambient.Ambient.X);
        fields.ColorEdit3("Ambient Ground"u8, ref ambient.AmbientGround.X);
        fields.ToggleDefault();
        fields.DragFloat("Exposure"u8, Float1, ref light.Specular, 0.01f, 0.0f, 2.0f, "%.3f");

        ImGui.PopID();
        if (fields.HasEdited(out var field)) _editedField = field;
    }

    private void DrawFogState()
    {
        ImGui.PushID("fog"u8);

        var fields = FormFieldInputs.MakeVertical();

        ref var fog = ref State.Data.Fog;
        fields.ColorEdit3("FogColor"u8, ref fog.Color.X);
        fields.DragFloat("Density"u8, Float1, ref fog.Density, 1f, 100, 1500, "%.5f");

        fields.ToggleDefault();

        ImGui.SeparatorText("Fog Height"u8);
        fields.DragFloat("Base"u8, Float1, ref fog.BaseHeight, 1f, -1000f, 1000f, "%.1f");
        fields.DragFloat("Falloff"u8, Float1, ref fog.HeightFalloff, 1f, 0.001f, 10000.0f, "%.3f");
        fields.DragFloat("Influence"u8, Float1, ref fog.HeightInfluence, 0.001f, 0f, 1f, "%.3f");


        ImGui.SeparatorText("Fog Optics"u8);
        fields.DragFloat("Scattering"u8, Float1, ref fog.Scattering, 0.001f, 0.0f, 1.0f, "%.3f");
        fields.DragFloat("Max Distance"u8, Float1, ref fog.MaxDistance, 1f, 1f, 10000f, "%.0f");
        fields.DragFloat("Strength"u8, Float1, ref fog.Strength, 0.001f, 0f, 1f, "%.3f");

        ImGui.PopID();

        if (fields.HasEdited(out var field)) _editedField = field;
    }

    private void DrawPostEffects()
    {
        ref var post = ref State.Data.PostEffect;
        ref var grade = ref post.Grade;
        ref var wb = ref post.WhiteBalance;
        ref var bloom = ref post.Bloom;
        ref var fx = ref post.ImageFx;

        var fields = new FormFieldInputs(0, false);

        ImGui.PushID("post"u8);

        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        fields.SliderFloat("Exposure"u8, Float1, ref grade.Exposure, 0.5f, 2f, "%.2f");
        fields.SliderFloat("Saturation"u8, Float1, ref grade.Saturation, 0f, 1.5f, "%.2f");
        fields.SliderFloat("Contrast"u8, Float1, ref grade.Contrast, 0f, 1.5f, "%.2f");
        fields.SliderFloat("Warmth"u8, Float1, ref grade.Warmth, 0f, 1f, "%.2f");
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        fields.SliderFloat("Tint"u8, Float1, ref wb.Tint, 0f, 1f, "%.2f");
        fields.SliderFloat("Strength"u8, Float1, ref wb.Strength, -1f, 1f, "%.2f");
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        fields.SliderFloat("Intensity"u8, Float1, ref bloom.Intensity, 0f, 2f, "%.2f");
        fields.SliderFloat("Threshold"u8, Float1, ref bloom.Threshold, 0f, 2f, "%.2f");
        fields.SliderFloat("Radius"u8, Float1, ref bloom.Radius, 0f, 10f, "%.2f");
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        fields.SliderFloat("Vignette"u8, Float1, ref fx.Vignette, 0f, 0.5f, "%.2f");
        fields.SliderFloat("Grain"u8, Float1, ref fx.Grain, 0f, 0.5f, "%.2f");
        fields.SliderFloat("xSharpen"u8, Float1, ref fx.Sharpen, 0f, 0.5f, "%.2f");
        fields.SliderFloat("Rolloff"u8, Float1, ref fx.Rolloff, 0f, 0.5f, "%.2f");
        ImGui.EndGroup();

        ImGui.PopID();

        if (fields.HasEdited(out var field)) _editedField = field;
    }
}